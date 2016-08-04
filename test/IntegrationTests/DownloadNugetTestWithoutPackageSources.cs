using DotNetCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestClass]
    public class DownloadNugetTestWithoutPackageSources
    {
        private static NugetDownloader downloader;
        private static CommandDirectoryCleanup commandDirectoryCleanup;
        private static string tempPath;

        [ClassInitialize]
#pragma warning disable CC0057 // Unused parameters
        public static void ClassInitialize(TestContext tc)
#pragma warning restore CC0057 // Unused parameters
        {
            tempPath = Path.Combine(Path.GetTempPath(), "DotNetTempPath" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10));
            Directory.CreateDirectory(tempPath);
            Directory.SetCurrentDirectory(tempPath);
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            downloader = new NugetDownloader(commandDirectoryCleanup.CommandDirectory);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            downloader.Dispose();
            commandDirectoryCleanup.Dispose();
            try
            {
                Directory.Delete(tempPath, true);
            }
#pragma warning disable CC0004 // Catch block cannot be empty
            catch { }
#pragma warning restore CC0004 // Catch block cannot be empty
        }

        [TestMethod]
        public async Task DownloadDotNetFooWithoutPackageSourcesAsync()
        {
            var directory = await downloader.DownloadAndExtractNugetAsync("dotnet-foo", force: false, includePreRelease: false);
            Directory.Exists(directory).Should().BeTrue();
            var cmd = Directory.EnumerateFiles(directory, "dotnet-foo.cmd", SearchOption.AllDirectories).FirstOrDefault();
            cmd.Should().NotBeNull();
            Directory.GetParent(cmd).Name.Should().Be("tools");
        }
    }
}