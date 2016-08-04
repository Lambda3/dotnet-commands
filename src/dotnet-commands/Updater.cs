using NuGet.Versioning;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCommands
{
    public class Updater
    {
        private readonly CommandDirectory commandDirectory;

        public Updater(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
        }

        public async Task<bool> UpdateAsync(string packageName, bool force, bool includePreRelease)
        {
            if (await ShouldntUpdateAsync(packageName, includePreRelease)) return true;
            var uninstaller = new Uninstaller(commandDirectory);
            var uninstalled = await uninstaller.UninstallAsync(packageName);
            if (!uninstalled) return false;
            var installer = new Installer(commandDirectory);
            var installed = await installer.InstallAsync(packageName, force, includePreRelease);
            return installed;
        }

        private async Task<bool> ShouldntUpdateAsync(string packageName, bool includePreRelease)
        {
            SemanticVersion largestAvailableVersion;
            using (var downloader = new NugetDownloader(commandDirectory))
                largestAvailableVersion = SemanticVersion.Parse(await downloader.GetLatestVersionAsync(packageName, includePreRelease));
            var directory = commandDirectory.GetDirectoryForPackage(packageName);
            var packageDirs = Directory.EnumerateDirectories(directory);
            var packageVersions = packageDirs.Select(packageDir => SemanticVersion.Parse(Path.GetFileName(packageDir)));
            var largestInstalledVersion = packageVersions.Max();
            return largestInstalledVersion >= largestAvailableVersion;
        }
    }
}