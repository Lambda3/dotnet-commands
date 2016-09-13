using DotNetCommands;
using FluentAssertions;
using NuGet.Versioning;
using NUnit.Framework;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static IntegrationTests.Retrier;

namespace IntegrationTests
{
    [TestFixture]
    public class InstallerTestForGenericToolWithSpecificVersion
    {
        private const string packageName = "dotnet-foo";
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private Installer installer;
        private bool installed;
        private string baseDir;

        [OneTimeSetUp]
        public Task OneTimeSetUp() => RetryAsync(SetupAsync);

        public async Task SetupAsync()
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            installed = await installer.InstallAsync(packageName, SemanticVersion.Parse("1.0.1"), force: false, includePreRelease: false);
            installed.Should().BeTrue();
        }

        [OneTimeTearDown]
        public void ClassCleanup() => commandDirectoryCleanup.Dispose();

        [Test]
        public void WroteRedirectFileForWindows()
        {
            var wroteRedirectFile = File.Exists(Path.Combine(baseDir, "bin", $"{packageName}.cmd"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                wroteRedirectFile.Should().BeTrue();
            else
                wroteRedirectFile.Should().BeFalse();
        }

        [Test]
        public void WroteRedirectFileForOtherPlatforms()
        {
            var wroteRedirectFile = File.Exists(Path.Combine(baseDir, "bin", packageName));
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                wroteRedirectFile.Should().BeTrue();
            else
                wroteRedirectFile.Should().BeFalse();
        }

        [Test]
        public void DidNotCreateRuntimeConfigDevJsonFileWithCorrectConfig() =>
            Directory.EnumerateFiles(baseDir, "*.runtimeconfig.dev.json", SearchOption.AllDirectories).Should().BeEmpty();

        [Test]
        public void InstalledCorrectPackageVersion() =>
            Directory.Exists(commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName, SemanticVersion.Parse("1.0.1"))).Should().BeTrue();
    }
}