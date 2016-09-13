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
        private readonly IList<SourceInfo> sourceInfos;

        public NugetDownloader(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
            var sources = GetSources();
            sourceInfos = sources.Select(s => new SourceInfo(s)).ToList();
        }

        private class PackageSourceComparer : IEqualityComparer<PackageSource>
        {
            public bool Equals(PackageSource x, PackageSource y) => string.Compare(x.Source, y.Source, true) == 0;
            public int GetHashCode(PackageSource source) => source.Source.GetHashCode() * 13;
        }

        private static readonly PackageSourceComparer packageSourceComparer = new PackageSourceComparer();

        private static IList<PackageSource> GetSources()
        {
            var settings = Settings.LoadDefaultSettings(Directory.GetCurrentDirectory(), configFileName: null, machineWideSettings: new MachineWideSettings());
            var sourceProvider = new PackageSourceProvider(settings);
            var sources = sourceProvider.LoadPackageSources().Where(s => s.ProtocolVersion == 3 || s.Source.EndsWith(@"/v3/index.json")).Distinct(packageSourceComparer).ToList();
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

        private async Task<NugetVersion> GetSpecificVersionAndSourceInfoAsync(string packageName, SemanticVersion packageVersion)
        {
            foreach (var sourceInfo in sourceInfos)
            {
                WriteLineIfVerbose($"Searching for '{packageName}@{packageVersion.ToString()}' on '{sourceInfo.Source.Name}'...");
                NugetVersion latestNugetVersion;
                try
                {
                    latestNugetVersion = await sourceInfo.GetPackageAsync(packageName, packageVersion, true);
                }
                catch (Exception ex)
                {
                    WriteLine($"Error when getting '{packageName}@{packageVersion.ToString()}'. Source used: '{sourceInfo.Source.Name}'.");
                    WriteLineIfVerbose($"Error details: {ex.ToString()}'.");
                    return null;
                }
                if (latestNugetVersion == null)
                {
                    WriteLineIfVerbose($"Could not get a version for '{packageName}@{packageVersion.ToString()}' on '{sourceInfo.Source.Name}'.");
                    continue;
                }
                else
                {
                    WriteLineIfVerbose($"Found version '{latestNugetVersion.Version}' for '{packageName}@{packageVersion.ToString()}' on '{sourceInfo.Source.Name}'.");
                    return latestNugetVersion;
                }
            }
            WriteLine($"Package '{packageName}' not found. Sources used:");
            foreach (var source in sourceInfos.Select(p => p.Source))
                WriteLine($" - {source.Name}: {source.Source}");
            return null;
        }

        private async Task<NugetVersion> GetLatestVersionAndSourceInfoAsync(string packageName, bool includePreRelease)
        {
            NugetVersion currentNugetVersion = null;
            foreach (var sourceInfo in sourceInfos)
            {
                WriteLineIfVerbose($"Searching for '{packageName}' on '{sourceInfo.Source.Name}'...");
                NugetVersion latestNugetVersion;
                try
                {
                    latestNugetVersion = await sourceInfo.GetPackageAsync(packageName, null, includePreRelease);
                }
                catch (Exception ex)
                {
                    WriteLine($"Error when getting '{packageName}'. Source used: '{sourceInfo.Source.Name}'.");
                    WriteLineIfVerbose($"Error details: {ex.ToString()}'.");
                    return null;
                }
                if (latestNugetVersion == null)
                {
                    WriteLineIfVerbose($"Could not get a version for '{packageName}' on '{sourceInfo.Source.Name}'.");
                    continue;
                }
                else
                {
                    WriteLineIfVerbose($"Found version '{latestNugetVersion.Version}' for '{packageName}' on '{sourceInfo.Source.Name}'.");
                }
                if (currentNugetVersion == null || latestNugetVersion.Version > currentNugetVersion.Version)
                    currentNugetVersion = latestNugetVersion;
            }
            if (currentNugetVersion == null)
            {
                WriteLine($"Package '{packageName}' not found. Sources used:");
                foreach (var source in sourceInfos.Select(p => p.Source))
                    WriteLine($" - {source.Name}: {source.Source}");
                return null;
            }
            WriteLineIfVerbose($"Latest version for '{packageName}' is '{currentNugetVersion.Version}' from '{currentNugetVersion.SourceInfo.Source.Name}'.");
            return currentNugetVersion;
        }

        /// <summary>
        /// Downloads the specified nupkg and extracts it to a directory
        /// </summary>
        /// <param name="packageName">The command to download, i.e. "dotnet-foo".</param>
        /// <param name="packageVersion">The version to install. If null, then latest will be used.</param>
        /// <param name="force">Force the download be made again if it was already downloaded earlier.</param>
        /// <param name="includePreRelease">Allow pre-release versions.</param>
        /// <returns>The directory where it is extracted</returns>
        public async Task<PackageInfo> DownloadAndExtractNugetAsync(string packageName, SemanticVersion packageVersion, bool force, bool includePreRelease)
        {
            if (!sourceInfos.Any())
            {
                WriteLine("No NuGet sources found.");
                return null;
            }
            var nugetVersion = packageVersion == null
                ? await GetLatestVersionAndSourceInfoAsync(packageName, includePreRelease)
                : await GetSpecificVersionAndSourceInfoAsync(packageName, packageVersion);
            if (nugetVersion == null)
            {
                WriteLineIfVerbose($"Could not get latest version for package '{packageName}'.");
                return null;
            }
            var nupkgResponse = await nugetVersion.GetNupkgAsync(nugetVersion.PackageName);
            if (nupkgResponse == null)
            {
                WriteLineIfVerbose($"Could not get a valid response for the nupkg download.");
                return null;
            }
            if (!nupkgResponse.IsSuccessStatusCode)
            {
                WriteLineIfVerbose($"Did not get a successful status code. Got {(int)nupkgResponse.StatusCode} ({nupkgResponse.StatusCode}).");
                return null;
            }
            var tempFilePath = Path.GetTempFileName();
            WriteLineIfVerbose($"Saving to '{tempFilePath}'.");
            using (var tempFileStream = File.OpenWrite(tempFilePath))
                await nupkgResponse.Content.CopyToAsync(tempFileStream);
            var destinationDir = commandDirectory.GetDirectoryForPackage(nugetVersion.PackageName, nugetVersion.Version);
            if (force)
                Directory.Delete(destinationDir, true);
            var shouldExtract = force || !Directory.Exists(destinationDir);
            if (shouldExtract)
            {
                WriteLineIfVerbose($"Extracting to '{destinationDir}'.");
                System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, destinationDir);
                foreach (var fileToRename in Directory.EnumerateFiles(destinationDir, "*.removeext", SearchOption.AllDirectories))
                    File.Move(fileToRename, fileToRename.Substring(0, fileToRename.Length - ".removeext".Length));
            }
            else
            {
                WriteLineIfVerbose($"Directory '{destinationDir}' already exists.");
            }
            var packageInfo = await PackageInfo.GetMainFilePathAsync(nugetVersion.PackageName, destinationDir);
            return packageInfo;
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
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("version")]
            public string Version { get; set; }
            [JsonProperty("versions")]
            public IList<ServiceDataVersion> Versions { get; set; }
        }

        private class ServiceDataVersion
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

            public async Task<NugetVersion> GetPackageAsync(string packageName, SemanticVersion packageVersion, bool includePreRelease)
            {
                var currentFeed = await GetFeedAsync();
                if (currentFeed == null)
                {
                    WriteLine("Current feed is null. Returning.");
                    return null;
                }
                var searchQueryServiceUrl = currentFeed.Resources.FirstOrDefault(r => r.Type == "SearchQueryService")?.Id;
                if (searchQueryServiceUrl == null)
                    searchQueryServiceUrl = currentFeed.Resources.FirstOrDefault(r => r.Type == "SearchQueryService/3.0.0-rc")?.Id;
                string serviceUrl;
                bool supportsQueryById;
                if (searchQueryServiceUrl == null)
                {
                    searchQueryServiceUrl = currentFeed.Resources.FirstOrDefault(r => r.Type == "SearchQueryService/3.0.0-beta")?.Id; //vsts is still in this version
                    if (searchQueryServiceUrl == null)
                    {
                        WriteLine("Nuget server does not offer a search query service we can work with.");
                        return null;
                    }
                    serviceUrl = $"{searchQueryServiceUrl}?q={packageName}{(includePreRelease ? "&prerelease=true" : "")}";
                    supportsQueryById = false;
                }
                else
                {
                    serviceUrl = $"{searchQueryServiceUrl}?q=packageid:{packageName}{(includePreRelease ? "&prerelease=true" : "")}";
                    supportsQueryById = true;
                }
                var serviceResponse = await HttpClient.GetAsync(serviceUrl);
                if (!serviceResponse.IsSuccessStatusCode)
                {
                    WriteLine($"Could not get service details from '{serviceUrl}'.");
                    WriteLineIfVerbose($"Got status code: '{(int)serviceResponse.StatusCode}' ({serviceResponse.StatusCode}).");
                    throw new Exception($"Could not get service details from '{serviceUrl}'.");
                }
                var serviceContent = await serviceResponse.Content.ReadAsStringAsync();
                var service = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<Service>(serviceContent));
                var serviceData = supportsQueryById
                    ? service.Data.FirstOrDefault()
                    : service.Data.FirstOrDefault(sd => string.Compare(sd.Id, packageName, true) == 0);
                var latest = serviceData?.Version;
                if (latest == null)
                {
                    WriteLineIfVerbose($"There was no package info for '{packageName}' on '{Source.Name}'.");
                    return null;
                }
                WriteLineIfVerbose($"Found package '{packageName}' with latest version {latest}.");
                var currentSemanticVersion = SemanticVersion.Parse(latest);
                if (packageVersion != null)
                {
                    var versionExists = serviceData.Versions.Any(v => v.Version == packageVersion.ToString());
                    if (versionExists)
                    {
                        currentSemanticVersion = packageVersion;
                    }
                    else
                    {
                        WriteLineIfVerbose($"Version '{packageVersion.ToString()}' was not found for '{packageName}' on '{Source.Name}'.");
                        return null;
                    }
                }
                var nugetVersion = new NugetVersion(currentSemanticVersion, this, serviceData.Id);
                return nugetVersion;
            }

            public PackageSource Source { get; set; }
            public HttpClient HttpClient { get; } = new HttpClient();

            private void UpdateAuthorizationForClient()
            {
                if (Source.Credentials != null)
                {
                    string password = null;
                    if (Source.Credentials.IsPasswordClearText)
                    {
                        password = Source.Credentials.PasswordText;
                    }
                    else
                    {
                        try
                        {
                            password = Source.Credentials.Password;
                        }
                        catch (Exception ex)
                        {
                            WriteLine($"Could not get password for source '{Source.Name}'.");
                            WriteLineIfVerbose(ex.ToString());
                            throw;
                        }
                    }
                    HttpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Source.Credentials.Username}:{password}")));
                }
                else
                {
                    HttpClient.DefaultRequestHeaders.Authorization = null;
                }
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
            public NugetVersion(SemanticVersion version, SourceInfo info, string packageName)
            {
                Version = version;
                SourceInfo = info;
                PackageName = packageName;
            }

            public Task<HttpResponseMessage> GetNupkgAsync(string packageName) => SourceInfo.GetNupkgAsync(Version, packageName);

            public SemanticVersion Version { get; }
            public SourceInfo SourceInfo { get; }
            public string PackageName { get; }
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