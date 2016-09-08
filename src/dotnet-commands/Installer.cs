using System.Threading.Tasks;
using Newtonsoft.Json;
using static DotNetCommands.Logger;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DotNetCommands
{
    public class Installer
    {
        private readonly CommandDirectory commandDirectory;
        public Installer(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
        }

        public async Task<bool> InstallAsync(string packageName, bool force, bool includePreRelease)
        {
            WriteLineIfVerbose($"Installing {packageName}...");
            PackageInfo packageInfo;
            try
            {
                using (var downloader = new NugetDownloader(commandDirectory))
                {
                    packageInfo = await downloader.DownloadAndExtractNugetAsync(packageName, force, includePreRelease);
                    if (packageInfo == null) return false;
                }
            }
            catch (Exception ex)
            {
                WriteLineIfVerbose("Could not download Nuget.");
                WriteLineIfVerbose(ex.ToString());
                return false;
            }
            var created = CreateBinFile(packageInfo);
            if (!created)
            {
                if (Directory.Exists(packageInfo.PackageDir))
                {
                    WriteLineIfVerbose($"Deleting {packageInfo}...");
                    Directory.Delete(packageInfo.PackageDir, true);
                    var packageDirectoryForAllVersions = commandDirectory.GetDirectoryForPackage(packageName);
                    if (Directory.EnumerateDirectories(packageDirectoryForAllVersions).Count() <= 1)
                        Directory.Delete(packageDirectoryForAllVersions, true);
                }
                return false;
            }
            var added = CreateRuntimeConfigDevJsonFile(packageInfo.PackageDir);
            if (!added) return false;
            var restored = await RestoreAsync(packageInfo.PackageDir);
            return restored;
        }


        private static bool CreateRuntimeConfigDevJsonFile(string packageDir)
        {
            var libDir = Path.Combine(packageDir, "lib");
            if (!Directory.Exists(libDir)) return true;
            var commandDlls = Directory.EnumerateFiles(libDir, "*.dll", SearchOption.AllDirectories);
            foreach (var commandDll in commandDlls)
            {
                var command = Path.GetFileNameWithoutExtension(commandDll);
                var runtimeConfigDevJsonFile = $"{command}.runtimeconfig.dev.json";
                var runtimeConfigDevJsonFullPath = Path.Combine(Path.GetDirectoryName(commandDll), runtimeConfigDevJsonFile);
                if (File.Exists(runtimeConfigDevJsonFullPath))
                {
                    WriteLineIfVerbose($"File '{runtimeConfigDevJsonFullPath}' already exists, not creating.");
                    return true;
                }
                var runtimeConfigJsonFile = $"{command}.runtimeconfig.json";
                var runtimeConfigJsonFullPath = Path.Combine(Path.GetDirectoryName(commandDll), runtimeConfigJsonFile);
                if (!File.Exists(runtimeConfigJsonFullPath))
                    continue;
                var homeDir = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("userprofile");
                if (string.IsNullOrWhiteSpace(homeDir))
                {
                    WriteLineIfVerbose("Could not find home dir.");
                    return false;
                }
                WriteLineIfVerbose($"Creating '{runtimeConfigDevJsonFile}'");
                var packagesDir = Path.Combine(homeDir, ".nuget", "packages");
                var escapedPackagesDir = JsonConvert.SerializeObject(packagesDir);
                escapedPackagesDir = escapedPackagesDir.Substring(1, escapedPackagesDir.Length - 2);
                File.WriteAllText(runtimeConfigDevJsonFullPath, @"{
  ""runtimeOptions"": {
    ""additionalProbingPaths"": [ """ + escapedPackagesDir + @""" ]
  }
}");
                WriteLineIfVerbose($"Wrote '{runtimeConfigDevJsonFullPath}' for correct probing paths.");
            }
            return true;
        }

        private async static Task<bool> RestoreAsync(string packageDir)
        {
            var libDir = Path.Combine(packageDir, "lib");
            if (!Directory.Exists(libDir)) return true;
            var projectJsons = Directory.EnumerateFiles(libDir, "project.json", SearchOption.AllDirectories);
            foreach (var projectJson in projectJsons)
            {
                WriteLineIfVerbose($"Restoring '{projectJson}'");
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    FileName = "dotnet",
                    Arguments = "restore",
                    WorkingDirectory = Path.GetDirectoryName(projectJson)
                };
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    var textResult = await process.StandardOutput.ReadToEndAsync();
                    WriteLine(textResult);
                    process.WaitForExit();
                    if (process.ExitCode != 0) return false;
                }
            }
            return true;
        }

        private bool CreateBinFile(PackageInfo packageInfo)
        {
            if (packageInfo == null || !packageInfo.Commands.Any()) return false;
            foreach (var command in packageInfo.Commands)
            {
                var binFile = commandDirectory.GetBinFile(command.Name + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ""));
                var relativeMainFileName = commandDirectory.MakeRelativeToBaseDir(command.ExecutableFilePath);
                File.WriteAllText(binFile, RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                    ? $@"@""%~dp0\{relativeMainFileName}"" %*"
                                    : $"#!/usr/bin/env bash{Environment.NewLine}. \"$( cd \"$( dirname \"${{BASH_SOURCE[0]}}\" )\" && pwd )/{relativeMainFileName}\" \"$@\"");
                WriteLineIfVerbose($"Wrote redirect file '{binFile}' pointing to '{relativeMainFileName}'.");
            }
            return true;
        }
    }
}