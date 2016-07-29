using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static DotNetCommands.Logger;
using static System.Console;
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
        private const string feedUrl = "https://api.nuget.org/v3/index.json";

        public async Task<bool> InstallAsync(string command, bool force, bool includePreRelease)
        {
            WriteLineIfVerbose($"Installing {command}.");
            string destinationDir;
            using (var client = new HttpClient())
            {
                var feedResponse = await client.GetAsync(feedUrl);
                if (!feedResponse.IsSuccessStatusCode)
                {
                    WriteLine($"Could not get feed details from '{feedUrl}'.");
                    return false;
                }
                var feedContent = await feedResponse.Content.ReadAsStringAsync();
                var resources = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Feed>(feedContent));
                var searchQueryServiceUrl = resources.Resources.First(r => r.Type == "SearchQueryService").Id;
                var serviceUrl = $"{searchQueryServiceUrl}?q=packageid:{command}{(includePreRelease ? "&prerelease=true" : "")}";
                var serviceResponse = await client.GetAsync(serviceUrl);
                if (!serviceResponse.IsSuccessStatusCode)
                {
                    WriteLine($"Could not get service details from '{serviceUrl}'.");
                    return false;
                }
                var serviceContent = await serviceResponse.Content.ReadAsStringAsync();
                var service = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Service>(serviceContent));
                var version = service.Data.FirstOrDefault()?.Version;
                if (version == null)
                {
                    WriteLine("Could not find a version.");
                    return false;
                }
                WriteLineIfVerbose($"Found version {version}.");
                destinationDir = commandDirectory.GetDirectoryForPackage(command, version);
                if (Directory.Exists(destinationDir))
                {
                    WriteLineIfVerbose($"Directory '{destinationDir}' already exists.");
                    if (force)
                        Directory.Delete(destinationDir, true);
                    else
                        return true;
                }
                var packageBaseAddressUrl = resources.Resources.Last(r => r.Type.StartsWith("PackageBaseAddress")).Id;
                var nupkgUrl = $"{packageBaseAddressUrl}{command.ToLower()}/{version.ToLower()}/{command.ToLower()}.{version.ToLower()}.nupkg";
                WriteLineIfVerbose($"Nupkg url is '{nupkgUrl}'.");
                var nupkgResponse = await client.GetAsync(nupkgUrl);
                if (!nupkgResponse.IsSuccessStatusCode)
                {
                    WriteLine($"Could not get nupkg from '{nupkgUrl}'.");
                    return false;
                }
                var tempFilePath = Path.GetTempFileName();
                WriteLineIfVerbose($"Saving to '{tempFilePath}'.");
                using (var tempFileStream = File.OpenWrite(tempFilePath))
                    await nupkgResponse.Content.CopyToAsync(tempFileStream);
                WriteLineIfVerbose($"Extracting to '{destinationDir}'.");
                tempFilePath = @"C:\proj\dotnet-commands\src\dotnet-commands\bin\Debug\dotnet-commands.0.0.1-alpha1-build4.nupkg";
                System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, destinationDir);
            }
            var created = await CreateBinFileAsync(destinationDir);
            if (!created) return false;
            var restored = await RestoreAsync(destinationDir);
            return restored;
        }

        private async Task<bool> RestoreAsync(string destinationDir)
        {
            var projectJsons = Directory.EnumerateFiles(Path.Combine(destinationDir, "lib"), "project.json", SearchOption.AllDirectories);
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
            WriteLineIfVerbose($"Wrote redirect file '{relativeMainFileName}'.");
            return true;
        }

        private class CommandMetadata
        {
            [JsonProperty("main")]
            public string Main { get; set; }
        }

        private class Feed
        {
            [JsonProperty("resources")]
            public IList<Resource> Resources { get; set; }
        }

        private class Resource
        {
            [JsonProperty("@id")]
            public string Id { get; set; }
            [JsonProperty("@type")]
            public string Type { get; set; }
        }

        private class Service
        {
            [JsonProperty("data")]
            public IList<ServiceData> Data { get; set; }
        }

        private class ServiceData
        {
            [JsonProperty("version")]
            public string Version { get; set; }
        }
    }
}