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
        private NugetDownloader downloader;
        private CommandDirectoryCleanup commandDirectoryCleanup;

        [TestInitialize]
        public void Initialize()
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            downloader = new NugetDownloader(commandDirectoryCleanup.CommandDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            downloader.Dispose();
            commandDirectoryCleanup.Dispose();
        }

        [TestMethod]
        public async Task DownloadDotNetFoo()
        {
            var directory = await downloader.DownloadAndExtractNugetAsync("dotnet-foo", force: false, includePreRelease: false);
            Directory.Exists(directory).Should().BeTrue();
            var cmd = Directory.EnumerateFiles(directory, "dotnet-foo.cmd", SearchOption.AllDirectories).FirstOrDefault();
            cmd.Should().NotBeNull();
            Directory.GetParent(cmd).Name.Should().Be("tools");
        }
    }
}