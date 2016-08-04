using DotNetCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestClass]
    public class UninstallerTestForGenericTool
    {
        private const string packageName = "dotnet-foo";
        private static CommandDirectoryCleanup commandDirectoryCleanup;
        private static string baseDir;
        private static Uninstaller uninstaller;
        private static bool uninstalled;

        [ClassInitialize]
#pragma warning disable CC0057 // Unused parameters
        public static async Task ClassInitialize(TestContext tc)
#pragma warning restore CC0057 // Unused parameters
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
            installed.Should().BeTrue();
            uninstaller = new Uninstaller(commandDirectoryCleanup.CommandDirectory);
            uninstalled = await uninstaller.UninstallAsync(packageName);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            commandDirectoryCleanup.Dispose();
        }

        [TestMethod]
        public void UninstalledSuccessfully() => uninstalled.Should().BeTrue();

        [TestMethod]
        public void DeletedRedirectFile() => File.Exists(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).Should().BeFalse();

        [TestMethod]
        public void DeletedPackageDirectory() => Directory.Exists(Path.Combine(baseDir, "packages", packageName)).Should().BeFalse();
    }
}