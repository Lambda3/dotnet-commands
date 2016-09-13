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

        public async Task<UpdateResult> UpdateAsync(string packageName, bool force, bool includePreRelease)
        {
            var updateNeeded = await IsUpdateNeededAsync(packageName, includePreRelease);
            if (updateNeeded == UpdateNeeded.No) return UpdateResult.NotNeeded;
            if (updateNeeded == UpdateNeeded.PackageNotFound) return UpdateResult.PackageNotFound;
            var uninstaller = new Uninstaller(commandDirectory);
            var uninstalled = await uninstaller.UninstallAsync(packageName);
            if (!uninstalled) return UpdateResult.CouldntUninstall;
            var installer = new Installer(commandDirectory);
            var installed = await installer.InstallAsync(packageName, null, force, includePreRelease);
            return installed ? UpdateResult.Success : UpdateResult.UninstalledAndNotReinstalled;
        }

        public async Task<UpdateNeeded> IsUpdateNeededAsync(string packageName, bool includePreRelease)
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
                        return UpdateNeeded.PackageNotFound;
                    }
                    largestAvailableVersion = SemanticVersion.Parse(versionFound);
                }
            }
            catch (Exception ex)
            {
                WriteLine("Could not download Nuget.");
                WriteLineIfVerbose(ex.ToString());
                return UpdateNeeded.No;
            }
            var directory = commandDirectory.GetDirectoryForPackage(packageName);
            var packageDirs = Directory.EnumerateDirectories(directory);
            var packageVersions = packageDirs.Select(packageDir => SemanticVersion.Parse(Path.GetFileName(packageDir)));
            var largestInstalledVersion = packageVersions.Max();
            return largestInstalledVersion >= largestAvailableVersion ? UpdateNeeded.No : UpdateNeeded.Yes;
        }

        public enum UpdateResult
        {
            Success, NotNeeded, PackageNotFound, CouldntUninstall, UninstalledAndNotReinstalled
        }
        public enum UpdateNeeded
        {
            Yes, No, PackageNotFound
        }
    }
}