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
        private string currentDirectoryWhenTestStarted;

        [OneTimeSetUp]
        public void ClassInitialize()
        {
            currentDirectoryWhenTestStarted = Directory.GetCurrentDirectory();
            tempPath = Path.Combine(Path.GetTempPath(), "DotNetTempPath" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10));
            Directory.CreateDirectory(tempPath);
            Directory.SetCurrentDirectory(tempPath);
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            downloader = new NugetDownloader(commandDirectoryCleanup.CommandDirectory);
        }

        [OneTimeTearDown]
        public void ClassCleanup()
        {
            Directory.SetCurrentDirectory(currentDirectoryWhenTestStarted);
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
            var packageInfo = await downloader.DownloadAndExtractNugetAsync("dotnet-foo", force: false, includePreRelease: false);
            Directory.Exists(packageInfo.PackageDir).Should().BeTrue();
            var cmd = Directory.EnumerateFiles(packageInfo.PackageDir, "dotnet-foo.cmd", SearchOption.AllDirectories).FirstOrDefault();
            cmd.Should().NotBeNull();
            Directory.GetParent(cmd).Name.Should().Be("tools");
        }
    }
}