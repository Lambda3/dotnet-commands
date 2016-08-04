using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static DotNetCommands.Logger;

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

        private async Task<Feed> GetFeedAsync()
        {
            var feedResponse = await client.GetAsync(feedUrl);
            if (!feedResponse.IsSuccessStatusCode)
            {
                WriteLine($"Could not get feed details from '{feedUrl}'.");
                return null;
            }
            var feedContent = await feedResponse.Content.ReadAsStringAsync();
            var feed = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Feed>(feedContent));
            return feed;
        }

        public async Task<string> GetLatestVersion(string packageName, bool includePreRelease) =>
            await GetLatestVersion(packageName, includePreRelease, await GetFeedAsync());

        private async Task<string> GetLatestVersion(string packageName, bool includePreRelease, Feed resources)
        {
            var searchQueryServiceUrl = resources.Resources.First(r => r.Type == "SearchQueryService").Id;
            var serviceUrl = $"{searchQueryServiceUrl}?q=packageid:{packageName}{(includePreRelease ? "&prerelease=true" : "")}";
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
            return version;
        }


        /// <summary>
        /// Downloads the specified nupkg and extracts it to a directory
        /// </summary>
        /// <param name="packageName">The command to download, i.e. "dotnet-foo".</param>
        /// <param name="force">Force the download be made again if it was already downloaded earlier.</param>
        /// <param name="includePreRelease">Allow pre-release versions.</param>
        /// <returns>The directory where it is extracted</returns>
        public async Task<string> DownloadAndExtractNugetAsync(string packageName, bool force, bool includePreRelease)
        {
            var feed = await GetFeedAsync();
            var version = await GetLatestVersion(packageName, includePreRelease, feed);
            var destinationDir = commandDirectory.GetDirectoryForPackage(packageName, version);
            if (Directory.Exists(destinationDir))
            {
                WriteLineIfVerbose($"Directory '{destinationDir}' already exists.");
                if (force)
                    Directory.Delete(destinationDir, true);
                else
                    return destinationDir;
            }
            var packageBaseAddressUrl = feed.Resources.Last(r => r.Type.StartsWith("PackageBaseAddress")).Id;
            var nupkgUrl = $"{packageBaseAddressUrl}{packageName.ToLower()}/{version.ToLower()}/{packageName.ToLower()}.{version.ToLower()}.nupkg";
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