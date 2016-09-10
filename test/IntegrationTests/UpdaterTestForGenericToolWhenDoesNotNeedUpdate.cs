using DotNetCommands;
using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using static IntegrationTests.Retrier;
using System.Runtime.InteropServices;

namespace IntegrationTests
{
    [TestFixture]
    public class UpdaterTestForGenericToolWhenDoesNotNeedUpdate
    {
        private const string packageName = "dotnet-foo";
        private CommandDirectoryCleanup commandDirectoryCleanup;
        private string baseDir;
        private DateTime lastWriteTimeForBinFile;
        private DateTime lastWriteTimeForPackageDir;

        [OneTimeSetUp]
        public Task OneTimeSetUp() => RetryAsync(SetupAsync);

        public async Task SetupAsync()
        {
            commandDirectoryCleanup = new CommandDirectoryCleanup();
            baseDir = commandDirectoryCleanup.CommandDirectory.BaseDir;
            var installer = new Installer(commandDirectoryCleanup.CommandDirectory);
            var installed = await installer.InstallAsync(packageName, force: false, includePreRelease: false);
            installed.Should().BeTrue();
            GetLastWriteTimes();
            var updater = new Updater(commandDirectoryCleanup.CommandDirectory);
            var updateResult = await updater.UpdateAsync(packageName, force: false, includePreRelease: false);
            updateResult.Should().Be(Updater.UpdateResult.NotNeeded);
        }

        private void GetLastWriteTimes()
        {
            lastWriteTimeForBinFile = new FileInfo(Path.Combine(baseDir, "bin", $"{packageName}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).LastWriteTime;
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDir = Directory.EnumerateDirectories(directory).First();
            lastWriteTimeForPackageDir = new DirectoryInfo(packageDir).LastWriteTime;
        }

        [OneTimeTearDown]
        public void ClassCleanup() => commandDirectoryCleanup?.Dispose();

        [Test]
        public void DidNotUpdateRedirectFile() =>
            new FileInfo(Path.Combine(baseDir, "bin", $"{packageName}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : "")}")).LastWriteTime.Should().Be(lastWriteTimeForBinFile);

        [Test]
        public void DidNotUpdatePackageDir()
        {
            var directory = commandDirectoryCleanup.CommandDirectory.GetDirectoryForPackage(packageName);
            var packageDir = Directory.EnumerateDirectories(directory).First();
            new DirectoryInfo(packageDir).LastWriteTime.Should().Be(lastWriteTimeForPackageDir);
        }
    }
}