using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    public class InstallerTestForMultipleCommandsWithMetadata
    {
        private const string packageName = "dotnet-bar";
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private Installer installer;
        private bool installed;
        private string baseDir;

        [OneTimeSetUp]
        public async Task ClassInitialize()
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
        }

        [OneTimeTearDown]
        public void ClassCleanup() => commandDirectoryCleanup.Dispose();

        [Test]
        public void InstalledSuccessfully() => installed.Should().BeTrue();

        [Test]
        public void WroteRedirectFileForCommandA() => File.Exists(Path.Combine(baseDir, "bin", $"dotnet-bar-aa{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).Should().BeTrue();

        [Test]
        public void WroteRedirectFileForCommandB() => File.Exists(Path.Combine(baseDir, "bin", $"dotnet-bar-bb{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).Should().BeTrue();
    }
}