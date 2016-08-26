using DotNetCommands;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    public class ListerListsPackagesInstalled
    {
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private Mock<Action<string>> logger;
        private bool isVerbose;

        [OneTimeSetUp]
        public async Task ClassInitialize()
        {
            isVerbose = Logger.IsVerbose;
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            var baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync("dotnet-foo", force: false, includePreRelease: false);
            installed.Should().BeTrue();
            installed = await installer.InstallAsync("dotnet-bar", force: false, includePreRelease: false);
            installed.Should().BeTrue();
            logger = new Mock<Action<string>>();
            Logger.IsVerbose = false;
            Logger.SetLogger(logger.Object);
            var lister = new Lister(commandDirectoryCleanup.CommandDirectory);
            await lister.ListAsync();
        }

        [OneTimeTearDown]
        public void ClassCleanup()
        {
            Logger.IsVerbose = isVerbose;
            commandDirectoryCleanup.Dispose();
        }

        [Test]
        public void ListedPackagesAndCommands()
        {
#pragma warning disable CC0031 // Check for null before calling a delegate
            logger.Verify(l => l(
@"dotnet-bar (1.0.2)
  dotnet-bar-aa
  dotnet-bar-bb
dotnet-foo (1.0.0)
"
            ), Times.Once);
#pragma warning restore CC0031 // Check for null before calling a delegate
        }
    }
}