using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static IntegrationTests.Retrier;

namespace IntegrationTests
{
    [TestFixture]
    public class InstallerTestForDotNetTool
    {
        private const string packageName = "dotnet-baz";
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
            installed = await installer.InstallAsync(packageName, force: false, includePreRelease: true);
            installed.Should().BeTrue();
        }

        [OneTimeTearDown]
        public void ClassCleanup() => commandDirectoryCleanup.Dispose();

        [Test]
        public void WroteRedirectFile() => File.Exists(Path.Combine(baseDir, "bin", $"{packageName}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).Should().BeTrue();

        [Test]
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
