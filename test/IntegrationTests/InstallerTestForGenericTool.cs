using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    public class InstallerTestForGenericTool
    {
        private const string packageName = "dotnet-foo";
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
        public void ClassCleanup()
        {
            commandDirectoryCleanup.Dispose();
        }

        [Test]
        public void InstalledSuccessfully()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                installed.Should().BeTrue();
            else
                installed.Should().BeFalse();
        }

        [Test]
        public void WroteRedirectFile()
        {
            var wroteRedirectFile = File.Exists(Path.Combine(baseDir, "bin", $"{packageName}.cmd"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                wroteRedirectFile.Should().BeTrue();
            else
                wroteRedirectFile.Should().BeFalse();
        }

        [Test]
        public void DidNotCreateRuntimeConfigDevJsonFileWithCorrectConfig() =>
            Directory.EnumerateFiles(baseDir, "*.runtimeconfig.dev.json", SearchOption.AllDirectories).Should().BeEmpty();
    }
}