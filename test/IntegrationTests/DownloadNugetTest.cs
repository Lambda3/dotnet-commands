using DotNetCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestClass]
    public class DownloadNugetTest
    {
        private static NugetDownloader downloader;
        private static CommandDirectoryCleanup commandDirectoryCleanup;

        [ClassInitialize]
#pragma warning disable CC0057 // Unused parameters
        public static void ClassInitialize(TestContext tc)
#pragma warning restore CC0057 // Unused parameters
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            downloader = new NugetDownloader(commandDirectoryCleanup.CommandDirectory);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            downloader.Dispose();
            commandDirectoryCleanup.Dispose();
        }

        [TestMethod]
        public async Task DownloadDotNetFooAsync()
        {
            var directory = await downloader.DownloadAndExtractNugetAsync("dotnet-foo", force: false, includePreRelease: false);
            Directory.Exists(directory).Should().BeTrue();
            var cmd = Directory.EnumerateFiles(directory, "dotnet-foo.cmd", SearchOption.AllDirectories).FirstOrDefault();
            cmd.Should().NotBeNull();
            Directory.GetParent(cmd).Name.Should().Be("tools");
        }
    }
}