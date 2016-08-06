using DotNetCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestClass]
    public class InstallerTestForMultipleCommandsWithMetadata
    {
        private const string packageName = "dotnet-bar";
        private static CommandDirectoryCleanup commandDirectoryCleanup;
        private static Installer installer;
        private static bool installed;
        private static string baseDir;

        [ClassInitialize]
#pragma warning disable CC0057 // Unused parameters
        public static async Task ClassInitialize(TestContext tc)
#pragma warning restore CC0057 // Unused parameters
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
        }

        [ClassCleanup]
        public static void ClassCleanup() => commandDirectoryCleanup.Dispose();

        [TestMethod]
        public void InstalledSuccessfully() => installed.Should().BeTrue();

        [TestMethod]
        public void WroteRedirectFileForCommandA() => File.Exists(Path.Combine(baseDir, "bin", $"dotnet-bar-aa{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).Should().BeTrue();

        [TestMethod]
        public void WroteRedirectFileForCommandB() => File.Exists(Path.Combine(baseDir, "bin", $"dotnet-bar-bb{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).Should().BeTrue();
    }
}