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
    public class UpdaterTestForGenericToolWhenDoesNotNeedUpdateBecauseGreater
    {
        private const string packageName = "dotnet-foo";
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private bool updated;
        private string baseDir;
        private DateTime lastWriteTimeForBinFile;
        private DateTime lastWriteTimeForPackageDir;

        [OneTimeSetUp]
        public async Task ClassInitializeAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
            installed.Should().BeTrue();
            MoveToLaterVersion();
            GetLastWriteTimes();
            var updater = new Updater(commandDirectoryCleanup.CommandDirectory);
            updated = await updater.UpdateAsync(packageName, force: false, includePreRelease: false);
        }

        private void MoveToLaterVersion()
        {
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDir = Directory.EnumerateDirectories(directory).First();
            var version = Path.GetFileName(packageDir);
            var semanticVersion = SemanticVersion.Parse(version);
            var greaterVersion = new SemanticVersion(semanticVersion.Major + 1, semanticVersion.Minor, semanticVersion.Patch, semanticVersion.ReleaseLabels, semanticVersion.Metadata).ToString();
            var newPackageDir = Path.Combine(Directory.GetParent(packageDir).ToString(), greaterVersion);
            Directory.Move(packageDir, newPackageDir);
            var binFile = Path.Combine(baseDir, "bin", $"{packageName}.cmd");
            File.WriteAllText(binFile, File.ReadAllText(binFile).Replace(version, greaterVersion));
        }

        private void GetLastWriteTimes()
        {
            lastWriteTimeForBinFile = new FileInfo(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).LastWriteTime;
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDir = Directory.EnumerateDirectories(directory).First();
            lastWriteTimeForPackageDir = new DirectoryInfo(packageDir).LastWriteTime;
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
        public void DidNotUpdateRedirectFile()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            new FileInfo(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).LastWriteTime.Should().Be(lastWriteTimeForBinFile);
        }

        [Test]
        public void DidNotUpdatePackageDir()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDir = Directory.EnumerateDirectories(directory).First();
            new DirectoryInfo(packageDir).LastWriteTime.Should().Be(lastWriteTimeForPackageDir);
        }
    }
}