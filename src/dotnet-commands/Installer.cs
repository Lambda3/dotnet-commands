using System.Linq;
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
            string destinationDir;
            using (var downloader = new NugetDownloader(commandDirectory))
            {
                destinationDir = await downloader.DownloadAndExtractNugetAsync(command, force, includePreRelease);
                if (destinationDir == null) return false;
            }
            var created = await CreateBinFileAsync(destinationDir);
            if (!created) return false;
            var added = CreateRuntimeConfigDevJsonFile(destinationDir, command);
            if (!added) return false;
            var restored = await RestoreAsync(destinationDir);
            return restored;
        }


        private static bool CreateRuntimeConfigDevJsonFile(string destinationDir, string command)
        {
            var libDir = Path.Combine(destinationDir, "lib");
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

        private async static Task<bool> RestoreAsync(string destinationDir)
        {
            var libDir = Path.Combine(destinationDir, "lib");
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

        private async Task<bool> CreateBinFileAsync(string destinationDir)
        {
            var commandMetadataTextFilePath = Path.Combine(destinationDir, "content", "commandMetadata.json");
            string mainFilePath;
            if (File.Exists(commandMetadataTextFilePath))
            {
                var commandMetadataText = File.ReadAllText(commandMetadataTextFilePath);
                var commandMetadata = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<CommandMetadata>(commandMetadataText));
                mainFilePath = Path.GetFullPath(Path.Combine(destinationDir, commandMetadata.Main));
            }
            else
            {
                var toolsDir = Path.Combine(destinationDir, "tools");
                if (!Directory.Exists(toolsDir))
                {
                    WriteLine("This package does not have a tools directory.");
                    return false;
                }
                mainFilePath = Directory.EnumerateFiles(toolsDir, "*.exe")
                    .Union(Directory.EnumerateFiles(toolsDir, "*.cmd"))
                    .Union(Directory.EnumerateFiles(toolsDir, "*.ps1")).FirstOrDefault();
                if (string.IsNullOrEmpty(mainFilePath))
                {
                    WriteLine("This package does not offer any executable.");
                    return false;
                }
            }
            var mainFileName = Path.GetFileName(mainFilePath);
            if (!mainFileName.StartsWith("dotnet-"))
            {
                WriteLine("This package does not offer a .NET CLI extension tool.");
                return false;
            }
            var binFile = commandDirectory.GetBinFile(mainFileName);
            var relativeMainFileName = commandDirectory.MakeRelativeToBaseDir(mainFilePath);
            File.WriteAllText(binFile, $@"@""%~dp0\{relativeMainFileName}"" %*");
            WriteLineIfVerbose($"Wrote redirect file '{binFile}' pointing to '{relativeMainFileName}'.");
            return true;
        }

        private class CommandMetadata
        {
            [JsonProperty("main")]
            public string Main { get; set; }
        }

    }
}