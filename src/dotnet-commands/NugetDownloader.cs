using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static System.Console;
using static DotNetCommands.Logger;
using Newtonsoft.Json;
using System.IO;

namespace DotNetCommands
{
    public sealed class NugetDownloader : IDisposable
    {
        private readonly CommandDirectory commandDirectory;
        private readonly HttpClient client = new HttpClient();
        public NugetDownloader(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
        }

        private const string feedUrl = "https://api.nuget.org/v3/index.json";

        /// <summary>
        /// Downloads the specified nupkg and extracts it to a directory
        /// </summary>
        /// <param name="command">The command to download, i.e. "dotnet-foo".</param>
        /// <param name="force">Force the download be made again if it was already downloaded earlier.</param>
        /// <param name="includePreRelease">Allow pre-release versions.</param>
        /// <returns>The directory where it is extracted</returns>
        public async Task<string> DownloadAndExtractNugetAsync(string command, bool force, bool includePreRelease)
        {
            var feedResponse = await client.GetAsync(feedUrl);
            if (!feedResponse.IsSuccessStatusCode)
            {
                WriteLine($"Could not get feed details from '{feedUrl}'.");
                return null;
            }
            var feedContent = await feedResponse.Content.ReadAsStringAsync();
            var resources = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Feed>(feedContent));
            var searchQueryServiceUrl = resources.Resources.First(r => r.Type == "SearchQueryService").Id;
            var serviceUrl = $"{searchQueryServiceUrl}?q=packageid:{command}{(includePreRelease ? "&prerelease=true" : "")}";
            var serviceResponse = await client.GetAsync(serviceUrl);
            if (!serviceResponse.IsSuccessStatusCode)
            {
                WriteLine($"Could not get service details from '{serviceUrl}'.");
                return null;
            }
            var serviceContent = await serviceResponse.Content.ReadAsStringAsync();
            var service = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Service>(serviceContent));
            var version = service.Data.FirstOrDefault()?.Version;
            if (version == null)
            {
                WriteLine("Could not find a version.");
                return null;
            }
            WriteLineIfVerbose($"Found version {version}.");
            var destinationDir = commandDirectory.GetDirectoryForPackage(command, version);
            if (Directory.Exists(destinationDir))
            {
                WriteLineIfVerbose($"Directory '{destinationDir}' already exists.");
                if (force)
                    Directory.Delete(destinationDir, true);
                else
                    return destinationDir;
            }
            var packageBaseAddressUrl = resources.Resources.Last(r => r.Type.StartsWith("PackageBaseAddress")).Id;
            var nupkgUrl = $"{packageBaseAddressUrl}{command.ToLower()}/{version.ToLower()}/{command.ToLower()}.{version.ToLower()}.nupkg";
            WriteLineIfVerbose($"Nupkg url is '{nupkgUrl}'.");
            var nupkgResponse = await client.GetAsync(nupkgUrl);
            if (!nupkgResponse.IsSuccessStatusCode)
            {
                WriteLine($"Could not get nupkg from '{nupkgUrl}'.");
                return null;
            }
            var tempFilePath = Path.GetTempFileName();
            WriteLineIfVerbose($"Saving to '{tempFilePath}'.");
            using (var tempFileStream = File.OpenWrite(tempFilePath))
                await nupkgResponse.Content.CopyToAsync(tempFileStream);
            WriteLineIfVerbose($"Extracting to '{destinationDir}'.");
            System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, destinationDir);
            return destinationDir;
        }

        public void Dispose() => client.Dispose();

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
