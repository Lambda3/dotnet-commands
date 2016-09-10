using DotNetCommands;
using FluentAssertions;
using NuGet.Versioning;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static IntegrationTests.Retrier;

namespace IntegrationTests
{
    [TestFixture]
    public class UpdaterTestForGenericToolWhenNeedsUpdate
    {
        private const string packageName = "dotnet-foo";
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private string baseDir;
        private string version;

        [OneTimeSetUp]
        public Task OneTimeSetUp() => RetryAsync(SetupAsync);

        private async Task SetupAsync()
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
            MoveToPreviousVersion();
            installed.Should().BeTrue();
            var updater = new Updater(commandDirectoryCleanup.CommandDirectory);
            var updateResult = await updater.UpdateAsync(packageName, force: false, includePreRelease: false);
            updateResult.Should().Be(Updater.UpdateResult.Success);
        }

        private void MoveToPreviousVersion()
        {
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDir = Directory.EnumerateDirectories(directory).First();
            version = Path.GetFileName(packageDir);
            var semanticVersion = SemanticVersion.Parse(version);
            semanticVersion.Major.Should().BeGreaterOrEqualTo(1, "If version is zero then we cannot safely run the test.");
            var smallerVersion = new SemanticVersion(semanticVersion.Major - 1, semanticVersion.Minor, semanticVersion.Patch + 1, semanticVersion.ReleaseLabels, semanticVersion.Metadata).ToString();
            var newPackageDir = Path.Combine(Directory.GetParent(packageDir).ToString(), smallerVersion);
            Directory.Move(packageDir, newPackageDir);
            var binFile = Path.Combine(baseDir, "bin", $"{packageName}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}");
            File.WriteAllText(binFile, File.ReadAllText(binFile).Replace(version, smallerVersion));
        }

        [OneTimeTearDown]
        public void ClassCleanup() => commandDirectoryCleanup?.Dispose();

        [Test]
        public void UpdatedRedirectFile() =>
            File.ReadAllText(Path.Combine(baseDir, "bin", $"{packageName}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).Should().Contain(version);

        [Test]
        public void DidNotCreateRuntimeConfigDevJsonFileWithCorrectConfig() =>
            Directory.EnumerateFiles(baseDir, "*.runtimeconfig.dev.json", SearchOption.AllDirectories).Should().BeEmpty();

        [Test]
        public void UpdatedVersion()
        {
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDirectory = Directory.EnumerateDirectories(directory).Single();
            Path.GetFileName(packageDirectory).Should().Be(version);
        }
        //todo install test to smaller version
    }
}