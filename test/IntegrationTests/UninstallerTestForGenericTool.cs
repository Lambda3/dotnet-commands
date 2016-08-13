using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    public class UninstallerTestForGenericTool
    {
        private const string packageName = "dotnet-foo";
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private string baseDir;
        private Uninstaller uninstaller;
        private bool uninstalled;

        [OneTimeSetUp]
        public async Task ClassInitialize()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
            installed.Should().BeTrue();
            uninstaller = new Uninstaller(commandDirectoryCleanup.CommandDirectory);
            uninstalled = await uninstaller.UninstallAsync(packageName);
        }

        [OneTimeTearDown]
        public void ClassCleanup() => commandDirectoryCleanup?.Dispose();

        [Test]
        public void UninstalledSuccessfully()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            uninstalled.Should().BeTrue();
        }

        [Test]
        public void DeletedRedirectFile()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            File.Exists(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).Should().BeFalse();
        }

        [Test]
        public void DeletedPackageDirectory()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            Directory.Exists(Path.Combine(baseDir, "packages", packageName)).Should().BeFalse();
        }
    }
}