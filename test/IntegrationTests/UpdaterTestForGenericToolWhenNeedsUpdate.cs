using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using NuGet.Versioning;
using System.Runtime.InteropServices;

namespace IntegrationTests
{
    [TestFixture]
    public class UpdaterTestForGenericToolWhenNeedsUpdate
    {
        private const string packageName = "dotnet-foo";
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private bool updated;
        private string baseDir;
        private string version;

        [OneTimeSetUp]
        public async Task ClassInitialize()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
            MoveToPreviousVersion();
            installed.Should().BeTrue();
            var updater = new Updater(commandDirectoryCleanup.CommandDirectory);
            updated = await updater.UpdateAsync(packageName, force: false, includePreRelease: false);
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
            var binFile = Path.Combine(baseDir, "bin", $"{packageName}.cmd");
            File.WriteAllText(binFile, File.ReadAllText(binFile).Replace(version, smallerVersion));
        }

        [OneTimeTearDown]
        public void ClassCleanup() => commandDirectoryCleanup?.Dispose();

        [Test]
        public void UpdatedSuccessfully()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            updated.Should().BeTrue();
        }

        [Test]
        public void UpdatedRedirectFile()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            File.ReadAllText(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).Should().Contain(version);
        }

        [Test]
        public void DidNotCreateRuntimeConfigDevJsonFileWithCorrectConfig()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            Directory.EnumerateFiles(baseDir, "*.runtimeconfig.dev.json", SearchOption.AllDirectories).Should().BeEmpty();
        }

        [Test]
        public void UpdatedVersion()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDirectory = Directory.EnumerateDirectories(directory).Single();
            Path.GetFileName(packageDirectory).Should().Be(version);
        }
        //todo teste para instalar vesao menor
    }
}