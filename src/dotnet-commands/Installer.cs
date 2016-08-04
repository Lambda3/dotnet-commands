using System.Threading.Tasks;
using Newtonsoft.Json;
using static DotNetCommands.Logger;
using System.IO;
using System;
using System.Diagnostics;

namespace DotNetCommands
{
    public class Installer
    {
        private readonly CommandDirectory commandDirectory;
        public Installer(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
        }

        public async Task<bool> InstallAsync(string command, bool force, bool includePreRelease)
        {
            WriteLineIfVerbose($"Installing {command}...");
            string packageDir;
            using (var downloader = new NugetDownloader(commandDirectory))
            {
                packageDir = await downloader.DownloadAndExtractNugetAsync(command, force, includePreRelease);
                if (packageDir == null) return false;
            }
            var created = await CreateBinFileAsync(packageDir);
            if (!created) return false;
            var added = CreateRuntimeConfigDevJsonFile(packageDir, command);
            if (!added) return false;
            var restored = await RestoreAsync(packageDir);
            return restored;
        }


        private static bool CreateRuntimeConfigDevJsonFile(string packageDir, string command)
        {
            var libDir = Path.Combine(packageDir, "lib");
            if (!Directory.Exists(libDir)) return true;
            var comandDlls = Directory.EnumerateFiles(libDir, $"{command}.dll", SearchOption.AllDirectories);
            foreach (var commandDll in comandDlls)
            {
                var runtimeConfigDevJsonFile = $"{command}.runtimeconfig.dev.json";
                var runtimeConfigDevJsonFullPath = Path.Combine(Path.GetDirectoryName(commandDll), runtimeConfigDevJsonFile);
                if (File.Exists(runtimeConfigDevJsonFullPath))
                {
                    WriteLineIfVerbose($"File '{runtimeConfigDevJsonFullPath}' already exists, not creating.");
                    return true;
                }
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

        private async Task<bool> CreateBinFileAsync(string packageDir) //need to do linux version
        {
            var packageInfo = new PackageInfo(packageDir);
            var mainFilePath = await packageInfo.GetMainFilePathAsync();
            if (mainFilePath == null) return false;
            var binFile = commandDirectory.GetBinFile(Path.GetFileName(mainFilePath));
            var relativeMainFileName = commandDirectory.MakeRelativeToBaseDir(mainFilePath);
            File.WriteAllText(binFile, $@"@""%~dp0\{relativeMainFileName}"" %*");
            WriteLineIfVerbose($"Wrote redirect file '{binFile}' pointing to '{relativeMainFileName}'.");
            return true;
        }
    }
}