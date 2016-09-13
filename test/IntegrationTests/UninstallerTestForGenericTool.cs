using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static IntegrationTests.Retrier;

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
        public Task OneTimeSetUp() => RetryAsync(SetupAsync);

        public async Task SetupAsync()
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync(packageName, null, force: false, includePreRelease: false);
            installed.Should().BeTrue();
            uninstaller = new Uninstaller(commandDirectoryCleanup.CommandDirectory);
            uninstalled = await uninstaller.UninstallAsync(packageName);
            uninstalled.Should().BeTrue();
        }

        [OneTimeTearDown]
        public void ClassCleanup() => commandDirectoryCleanup?.Dispose();

        [Test]
        public void DeletedRedirectFile() =>
            File.Exists(Path.Combine(baseDir, "bin", $"{packageName}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).Should().BeFalse();

        [Test]
        public void DeletedPackageDirectory() =>
            Directory.Exists(Path.Combine(baseDir, "packages", packageName)).Should().BeFalse();
    }
}