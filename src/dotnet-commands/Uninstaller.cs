using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static DotNetCommands.Logger;

namespace DotNetCommands
{
    public class Uninstaller
    {
        private readonly CommandDirectory commandDirectory;

        public Uninstaller(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
        }

        public async Task<bool> UninstallAsync(string packageName)
        {
            var deleted = await DeleteRedirectFileAsync(packageName);
            if (!deleted) return false;
            return DeletePackageDirectory(packageName);
        }

        private async Task<bool> DeleteRedirectFileAsync(string packageName)
        {
            var packageDir = commandDirectory.GetDirectoryForPackage(packageName);
            if (!Directory.Exists(packageDir))
            {
                WriteLine($"Package {packageName} is not installed.");
                return false;
            }
            var packageDirs = Directory.EnumerateDirectories(packageDir);
            foreach (var packageAndVersionDir in packageDirs)
            {
                var packageInfo = await PackageInfo.GetMainFilePathAsync(packageName, packageAndVersionDir);
                if (packageInfo == null || !packageInfo.Commands.Any()) return true;
                foreach (var command in packageInfo.Commands)
                {
                    var binFile = commandDirectory.GetBinFile(command.Name + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ""));
                    try
                    {
                        if (File.Exists(binFile))
                        {
                            WriteLineIfVerbose($"Deleting bin file '{binFile}'.");
                            File.Delete(binFile);
                        }
                        else
                        {
                            WriteLineIfVerbose($"Bin file '{binFile}' does not exist.");
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLine($"Could not delete bin file '{binFile}'.");
                        WriteLineIfVerbose(ex.ToString());
                        return false;
                    }
                }
            }
            return true;
        }

        private bool DeletePackageDirectory(string packageName)
        {
            var packageDir = commandDirectory.GetDirectoryForPackage(packageName);
            var parent = Directory.GetParent(packageDir);
            var movedPackageDir = Path.Combine(parent.ToString(), $"{packageName}_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 5)}.safetodelete.tmp");
            try
            {
                //try to move, the package could be in use
                WriteLineIfVerbose($"Moving '{packageDir}' to '{movedPackageDir}' to later delete it.");
                Directory.Move(packageDir, movedPackageDir);
            }
            catch (Exception ex)
            {
                WriteLine($"Could not delete package '{packageDir}', it is probably in use or you do not have permission.");
                WriteLineIfVerbose(ex.ToString());
                return false;
            }
            try
            {
                WriteLineIfVerbose($"Deleting '{movedPackageDir}'.");
                Directory.Delete(movedPackageDir, true);
            }
            catch (Exception ex)
            {
                WriteLine($"Could not delete the moved package for '{packageDir}'. This is not expected and should not happen.");
                WriteLineIfVerbose(ex.ToString());
            }
            return true;
        }
    }
}