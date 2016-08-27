using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DotNetCommands.Logger;

namespace DotNetCommands
{
    public class Lister
    {
        private CommandDirectory commandDirectory;

        public Lister(CommandDirectory commandDirectory)
        {
            this.commandDirectory = commandDirectory;
        }

        public async Task<bool> ListAsync()
        {
            var sb = new StringBuilder();
            try
            {
                var packageDirectories = Directory.EnumerateDirectories(commandDirectory.PackagesDir).OrderBy(d => d);
                foreach (var pkgDir in packageDirectories)
                {
                    var packageDirectory = pkgDir;
                    if (!packageDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        packageDirectory += Path.DirectorySeparatorChar.ToString();
                    var packageName = Path.GetFileName(Path.GetDirectoryName(packageDirectory));
                    var versionDirectories = Directory.EnumerateDirectories(packageDirectory).OrderBy(d => d);
                    foreach (var verDir in versionDirectories)
                    {
                        var versionDirectory = verDir;
                        if (!versionDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                            versionDirectory += Path.DirectorySeparatorChar.ToString();
                        var packageVersion = Path.GetFileName(Path.GetDirectoryName(versionDirectory));
                        sb.AppendLine($"{packageName} ({packageVersion})");
                    }

                    var packageDirs = Directory.EnumerateDirectories(commandDirectory.GetDirectoryForPackage(packageName)).OrderBy(d => d);
                    foreach (var packageDir in packageDirs)
                    {
                        var packageInfo = await PackageInfo.GetMainFilePathAsync(packageName, packageDir);
                        if (packageInfo == null || !packageInfo.Commands.Any()) continue;
                        foreach (var command in packageInfo.Commands)
                        {
                            if (command.Name == packageName) continue;
                            if (IsVerbose)
                                sb.AppendLine($"  {command.Name} ({command.ExecutableFilePath})");
                            else
                                sb.AppendLine($"  {command.Name}");
                        }
                    }
                }
                var packagesInfo = sb.ToString();
                WriteLine(packagesInfo);
                return true;
            }
            catch (Exception ex)
            {
                WriteLineIfVerbose($"Error getting package info: '{ex.Message}'.");
                return false;
            }
        }
    }
}