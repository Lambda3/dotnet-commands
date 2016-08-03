using DotNetCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestClass]
    public class InstallerTestForGenericTool
    {
        private const string packageName = "dotnet-foo";
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
        public static void ClassCleanup()
        {
            commandDirectoryCleanup.Dispose();
        }

        [TestMethod]
        public void InstalledSuccessfully() => installed.Should().BeTrue();

        [TestMethod]
        public void WroteRedirectFile() => File.Exists(Path.Combine(baseDir, "bin", $"{packageName}.cmd")).Should().BeTrue();

        [TestMethod]
        public void DidNotCreateRuntimeConfigDevJsonFileWithCorrectConfig()
        {
            Directory.EnumerateFiles(baseDir, "*.runtimeconfig.dev.json", SearchOption.AllDirectories).Should().BeEmpty();
            //File.Exists(Path.Combine(baseDir, "packages", packageName, "1.0.0", $"{packageName}.cmd")).Should().BeTrue();
        }
    }
}
