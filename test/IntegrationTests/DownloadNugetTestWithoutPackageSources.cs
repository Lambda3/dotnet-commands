using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    public class DownloadNugetTestWithoutPackageSources
    {
        private NugetDownloader downloader;
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private string tempPath;

        [OneTimeSetUp]
        public void ClassInitialize()
        {
            tempPath = Path.Combine(Path.GetTempPath(), "DotNetTempPath" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10));
            Directory.CreateDirectory(tempPath);
            Directory.SetCurrentDirectory(tempPath);
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            downloader = new NugetDownloader(commandDirectoryCleanup.CommandDirectory);
        }

        [OneTimeTearDown]
        public void ClassCleanup()
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

        [Test]
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