using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Versioning;
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
        private readonly IList<PackageSource> sources;

        public NugetDownloader(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
            sources = GetSources();
        }

        private static IList<PackageSource> GetSources()
        {
            var settings = Settings.LoadDefaultSettings(Directory.GetCurrentDirectory(), configFileName: null, machineWideSettings: new MachineWideSettings());
            var sourceProvider = new PackageSourceProvider(settings);
            var sources = sourceProvider.LoadPackageSources().Where(s => s.ProtocolVersion == 3).ToList();
            return sources;
        }

        private async Task<Feed> GetFeedAsync(PackageSource source)
        {
            var feedUrl = source.SourceUri.ToString();
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

        public async Task<string> GetLatestVersionAsync(string packageName, bool includePreRelease)
        {
            if (!sources.Any())
            {
                WriteLine("No NuGet sources found.");
                return null;
            }
            return (await GetLatestVersionAndFeedAsync(packageName, includePreRelease)).Version.ToString();
        }

        private async Task<VersionAndFeed> GetLatestVersionAndFeedAsync(string packageName, bool includePreRelease)
        {
            Feed feed = null;
            var semanticVersion = default(SemanticVersion);
            foreach (var source in sources)
            {
                var currentFeed = await GetFeedAsync(source);
                var searchQueryServiceUrl = currentFeed.Resources.First(r => r.Type == "SearchQueryService").Id;
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
                    continue;
                }
                WriteLineIfVerbose($"Found version {version}.");
                var currentSemanticVersion = SemanticVersion.Parse(version);
                if (currentSemanticVersion > semanticVersion)
                {
                    feed = currentFeed;
                    semanticVersion = currentSemanticVersion;
                }
            }
            return new VersionAndFeed { Version = semanticVersion, Feed = feed };
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
            if (!sources.Any())
            {
                WriteLine("No NuGet sources found.");
                return null;
            }
            var versionAndFeed = await GetLatestVersionAndFeedAsync(packageName, includePreRelease);
            var packageBaseAddressUrl = versionAndFeed.Feed.Resources.Last(r => r.Type.StartsWith("PackageBaseAddress")).Id;
            var version = versionAndFeed.Version.ToString();
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
            var destinationDir = commandDirectory.GetDirectoryForPackage(packageName, version);
            if (Directory.Exists(destinationDir))
            {
                WriteLineIfVerbose($"Directory '{destinationDir}' already exists.");
                if (force)
                    Directory.Delete(destinationDir, true);
                else
                    return destinationDir;
            }
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

        private class VersionAndFeed
        {
            public SemanticVersion Version { get; set; }
            public Feed Feed { get; set; }
        }
    }


    public class MachineWideSettings : IMachineWideSettings
    {
        public MachineWideSettings()
        {
            var baseDirectory = NuGetEnvironment.GetFolderPath(NuGetFolderPath.MachineWideConfigDirectory);
            Settings = NuGet.Configuration.Settings.LoadMachineWideSettings(baseDirectory);
        }

        public IEnumerable<Settings> Settings { get; private set; }
    }
}