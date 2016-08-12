using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    public class DownloadNugetTest
    {
        private NugetDownloader downloader;
        private CommandDirectoryCleanup commandDirectoryCleanup;

        [OneTimeSetUp]
        public void ClassInitialize()
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            downloader = new NugetDownloader(commandDirectoryCleanup.CommandDirectory);
        }

        [OneTimeTearDown]
        public void ClassCleanup()
        {
            downloader.Dispose();
            commandDirectoryCleanup.Dispose();
        }

        [Test]
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