using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static DotNetCommands.Logger;

namespace DotNetCommands
{
    public sealed class NugetDownloader : IDisposable
    {
        private readonly CommandDirectory commandDirectory;
        //private readonly HttpClient client = new HttpClient();
        private IEnumerable<SourceInfo> sourceInfos;

        public NugetDownloader(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
            var sources = GetSources();
            sourceInfos = sources.Select(s => new SourceInfo(s));
        }

        private static IList<PackageSource> GetSources()
        {
            var settings = Settings.LoadDefaultSettings(Directory.GetCurrentDirectory(), configFileName: null, machineWideSettings: new MachineWideSettings());
            var sourceProvider = new PackageSourceProvider(settings);
            var sources = sourceProvider.LoadPackageSources().Where(s => s.ProtocolVersion == 3 || s.Source.EndsWith(@"/v3/index.json")).ToList();
            if (!sources.Any())
            {
                var source = new PackageSource("https://api.nuget.org/v3/index.json", "api.nuget.org", isEnabled: true, isOfficial: true, isPersistable: true)
                {
                    ProtocolVersion = 3
                };
                sources.Add(source);
            }
            return sources;
        }

        public async Task<string> GetLatestVersionAsync(string packageName, bool includePreRelease)
        {
            if (!sourceInfos.Any())
            {
                WriteLine("No NuGet sources found.");
                return null;
            }
            return (await GetLatestVersionAndSourceInfoAsync(packageName, includePreRelease))?.Version.ToString();
        }

        private async Task<NugetVersion> GetLatestVersionAndSourceInfoAsync(string packageName, bool includePreRelease)
        {
            NugetVersion currentNugetVersion = null;
            foreach (var sourceInfo in sourceInfos)
            {
                SemanticVersion latestVersion;
                try
                {
                    latestVersion = await sourceInfo.GetLatestVersionAsync(packageName, includePreRelease);
                }
                catch (Exception)
                {
                    return null;
                }
                if (latestVersion == null) continue;
                if (currentNugetVersion == null || latestVersion > currentNugetVersion.Version)
                    currentNugetVersion = new NugetVersion(latestVersion, sourceInfo);
            }
            if (currentNugetVersion == null)
            {
                WriteLine($"Package '{packageName}' not found. Sources used:");
                foreach (var source in sourceInfos.Select(p => p.Source))
                    WriteLine($" - {source.Name}: {source.Source}");
                return null;
            }
            return currentNugetVersion;
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
            if (!sourceInfos.Any())
            {
                WriteLine("No NuGet sources found.");
                return null;
            }
            var nugetVersion = await GetLatestVersionAndSourceInfoAsync(packageName, includePreRelease);
            var nupkgResponse = await nugetVersion.GetNupkgAsync(packageName);
            if (nupkgResponse == null)
            {
                WriteLineIfVerbose($"Could not get a valid response for the nupkg download.");
                return null;
            }
            if (!nupkgResponse.IsSuccessStatusCode)
                return null;
            var tempFilePath = Path.GetTempFileName();
            WriteLineIfVerbose($"Saving to '{tempFilePath}'.");
            using (var tempFileStream = File.OpenWrite(tempFilePath))
                await nupkgResponse.Content.CopyToAsync(tempFileStream);
            var destinationDir = commandDirectory.GetDirectoryForPackage(packageName, nugetVersion.Version.ToString());
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
            foreach (var fileToRename in Directory.EnumerateFiles(destinationDir, "*.removeext", SearchOption.AllDirectories))
                File.Move(fileToRename, fileToRename.Substring(0, fileToRename.Length - ".removeext".Length));
            return destinationDir;
        }

        public void Dispose()
        {
            foreach (var sourceInfo in sourceInfos)
                sourceInfo.Dispose();
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

        private class SourceInfo : IDisposable
        {
            private string FeedUrl { get; }

            public SourceInfo(PackageSource source)
            {
                Source = source;
                UpdateAuthorizationForClient();
                FeedUrl = source.SourceUri.ToString();
            }

            private Feed feed;
            public async Task<Feed> GetFeedAsync()
            {
                if (feed != null) return feed;
                var feedResponse = await HttpClient.GetAsync(FeedUrl);
                if (!feedResponse.IsSuccessStatusCode)
                {
                    WriteLine($"Could not get feed details from '{FeedUrl}'. Got status code '{feedResponse.StatusCode.ToString()}' ({(int)feedResponse.StatusCode}).");
                    return null;
                }
                var feedContent = await feedResponse.Content.ReadAsStringAsync();
                feed = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Feed>(feedContent));
                return feed;
            }

            public async Task<SemanticVersion> GetLatestVersionAsync(string packageName, bool includePreRelease)
            {
                var currentFeed = await GetFeedAsync();
                if (currentFeed == null) return null;
                var searchQueryServiceUrl = currentFeed.Resources.First(r => r.Type == "SearchQueryService").Id;
                var serviceUrl = $"{searchQueryServiceUrl}?q=packageid:{packageName}{(includePreRelease ? "&prerelease=true" : "")}";
                var serviceResponse = await HttpClient.GetAsync(serviceUrl);
                if (!serviceResponse.IsSuccessStatusCode)
                {
                    WriteLine($"Could not get service details from '{serviceUrl}'.");
                    throw new Exception($"Could not get service details from '{serviceUrl}'.");
                }
                var serviceContent = await serviceResponse.Content.ReadAsStringAsync();
                var service = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Service>(serviceContent));
                var version = service.Data.FirstOrDefault()?.Version;
                if (version == null)
                {
                    return null;
                }
                WriteLineIfVerbose($"Found version {version}.");
                var currentSemanticVersion = SemanticVersion.Parse(version);
                return currentSemanticVersion;
            }

            public PackageSource Source { get; set; }
            public HttpClient HttpClient { get; } = new HttpClient();

            private void UpdateAuthorizationForClient()
            {
                HttpClient.DefaultRequestHeaders.Authorization = Source.Credentials != null
                    ? new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Source.Credentials.Username}:{Source.Credentials.PasswordText}")))
                    : null;
            }

            public async Task<HttpResponseMessage> GetNupkgAsync(SemanticVersion semanticVersion, string packageName)
            {
                var packageBaseAddressUrl = feed.Resources.Last(r => r.Type.StartsWith("PackageBaseAddress")).Id;
                var version = semanticVersion.ToString();
                var serviceUrl = $"{packageBaseAddressUrl}{packageName.ToLower()}/{version.ToLower()}/{packageName.ToLower()}.{version.ToLower()}.nupkg";
                WriteLineIfVerbose($"Nupkg url is '{serviceUrl}'.");
                var originalUri = new Uri(serviceUrl);
                var url = serviceUrl;
                var redirects = 0;
                using (var localClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
                {
                    while (true)
                    {
                        if (redirects > 20)
                        {
                            WriteLineIfVerbose($"Exceeded maximum number of redirects.");
                            return null;
                        }
                        localClient.DefaultRequestHeaders.Authorization = originalUri.Host == new Uri(url).Host
                            ? HttpClient.DefaultRequestHeaders.Authorization
                            : null;
                        WriteLineIfVerbose($"Downloading nupkg from '{url}'...");
                        var serviceResponse = await localClient.GetAsync(url);
                        if (serviceResponse.StatusCode == System.Net.HttpStatusCode.Redirect
                            || serviceResponse.StatusCode == System.Net.HttpStatusCode.MovedPermanently
                            || serviceResponse.StatusCode == System.Net.HttpStatusCode.RedirectMethod)
                        {
                            url = serviceResponse.Headers.Location.ToString();
                            WriteLineIfVerbose($"Redirected to '{url}'.");
                            redirects++;
                        }
                        else
                        {
                            if (!serviceResponse.IsSuccessStatusCode)
                                WriteLineIfVerbose($"Got response code {(int)serviceResponse.StatusCode} ({serviceResponse.StatusCode}) when accessing '{url}'...");
                            return serviceResponse;
                        }
                    }
                }
            }

            public void Dispose() => HttpClient.Dispose();
        }

        private class NugetVersion
        {
            public NugetVersion(SemanticVersion version, SourceInfo info)
            {
                Version = version;
                SourceInfo = info;
            }

            public Task<HttpResponseMessage> GetNupkgAsync(string packageName) => SourceInfo.GetNupkgAsync(Version, packageName);

            public SemanticVersion Version { get; }
            public SourceInfo SourceInfo { get; }
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