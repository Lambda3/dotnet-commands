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
    public class UpdaterTestForGenericToolWhenNeedsUpdate
    {
        private const string packageName = "dotnet-foo";
        private static CommandDirectoryCleanup commandDirectoryCleanup;
        private static bool updated;
        private static string baseDir;
        private static string version;

        [ClassInitialize]
#pragma warning disable CC0057 // Unused parameters
        public static async Task ClassInitialize(TestContext tc)
#pragma warning restore CC0057 // Unused parameters
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
            MoveToPreviousVersion();
            installed.Should().BeTrue();
            var updater = new Updater(commandDirectoryCleanup.CommandDirectory);
            updated = await updater.UpdateAsync(packageName, force: false, includePreRelease: false);
        }

        private static void MoveToPreviousVersion()
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

        [ClassCleanup]
        public static void ClassCleanup()
        {
            commandDirectoryCleanup.Dispose();
        }

        [TestMethod]
        public void UpdatedSuccessfully() => updated.Should().BeTrue();

        [TestMethod]
        public void UpdatedRedirectFile() => File.ReadAllText(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).Should().Contain(version);

        [TestMethod]
        public void DidNotCreateRuntimeConfigDevJsonFileWithCorrectConfig() =>
            Directory.EnumerateFiles(baseDir, "*.runtimeconfig.dev.json", SearchOption.AllDirectories).Should().BeEmpty();

        [TestMethod]
        public void UpdatedVersion()
        {
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDirectory = Directory.EnumerateDirectories(directory).Single();
            Path.GetFileName(packageDirectory).Should().Be(version);
        }
        //todo teste para instalar vesao menor
    }
}