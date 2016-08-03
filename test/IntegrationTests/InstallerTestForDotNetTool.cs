using DotNetCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestClass]
    public class InstallerTestForDotNetTool
    {
        private const string packageName = "dotnet-commands";
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
            installed = await installer.InstallAsync(packageName, force: false, includePreRelease: true);
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
        public void CreatedRuntimeConfigDevJsonFileWithCorrectConfig()
        {
            var runtimeConfigFile = Directory.EnumerateFiles(Path.Combine(baseDir, "packages", packageName), $"{packageName}.runtimeconfig.dev.json", SearchOption.AllDirectories).SingleOrDefault();
            runtimeConfigFile.Should().NotBeNull();
            var parentDir = Directory.GetParent(runtimeConfigFile);
            parentDir.Name.Should().Be("netcoreapp1.0");
            parentDir.Parent.Name.Should().Be("lib");
            parentDir.Parent.Parent.Parent.Name.Should().Be(packageName);
        }
    }
}
