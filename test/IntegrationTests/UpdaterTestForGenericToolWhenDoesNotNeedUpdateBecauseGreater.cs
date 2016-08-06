using DotNetCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using NuGet.Versioning;

namespace IntegrationTests
{
    [TestClass]
    public class UpdaterTestForGenericToolWhenDoesNotNeedUpdateBecauseGreater
    {
        private const string packageName = "dotnet-foo";
        private static CommandDirectoryCleanup commandDirectoryCleanup;
        private static bool updated;
        private static string baseDir;
        private static DateTime lastWriteTimeForBinFile;
        private static DateTime lastWriteTimeForPackageDir;

        [ClassInitialize]
#pragma warning disable CC0057 // Unused parameters
        public static async Task ClassInitializeAsync(TestContext tc)
#pragma warning restore CC0057 // Unused parameters
        {
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

        private static void MoveToLaterVersion()
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

        private static void GetLastWriteTimes()
        {
            lastWriteTimeForBinFile = new FileInfo(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).LastWriteTime;
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDir = Directory.EnumerateDirectories(directory).First();
            lastWriteTimeForPackageDir = new DirectoryInfo(packageDir).LastWriteTime;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            commandDirectoryCleanup.Dispose();
        }

        [TestMethod]
        public void UpdatedSuccessfully() => updated.Should().BeTrue();

        [TestMethod]
        public void DidNotUpdateRedirectFile() => new FileInfo(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).LastWriteTime.Should().Be(lastWriteTimeForBinFile);

        [TestMethod]
        public void DidNotUpdatePackageDir()
        {
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDir = Directory.EnumerateDirectories(directory).First();
            new DirectoryInfo(packageDir).LastWriteTime.Should().Be(lastWriteTimeForPackageDir);
        }
    }
}