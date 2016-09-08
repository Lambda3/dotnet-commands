using NuGet.Versioning;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DotNetCommands.Logger;

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

        public async Task<bool> ShouldntUpdateAsync(string packageName, bool includePreRelease)
        {
            SemanticVersion largestAvailableVersion;
            try
            {
                using (var downloader = new NugetDownloader(commandDirectory))
                {
                    var versionFound = await downloader.GetLatestVersionAsync(packageName, includePreRelease);
                    if (versionFound == null)
                    {
                        WriteLineIfVerbose($"Could not find '{packageName}'.");
                        return true;
                    }
                    largestAvailableVersion = SemanticVersion.Parse(versionFound);
                }
            }
            catch (Exception ex)
            {
                WriteLine("Could not download Nuget.");
                WriteLineIfVerbose(ex.ToString());
                return true;
            }
            var directory = commandDirectory.GetDirectoryForPackage(packageName);
            var packageDirs = Directory.EnumerateDirectories(directory);
            var packageVersions = packageDirs.Select(packageDir => SemanticVersion.Parse(Path.GetFileName(packageDir)));
            var largestInstalledVersion = packageVersions.Max();
            return largestInstalledVersion >= largestAvailableVersion;
        }
    }
}