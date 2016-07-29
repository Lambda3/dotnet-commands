using System.IO;
using System.Linq;

namespace DotNetCommands
{
    public class CommandDirectory
    {
        private readonly string baseDir;
        private readonly string packagesDir;
        private readonly string binDir;
        public CommandDirectory(string baseDir)
        {
            this.baseDir = baseDir;
            this.packagesDir = Path.Combine(baseDir, "packages");
            this.binDir = Path.Combine(baseDir, "bin");
            if (!Directory.Exists(this.baseDir))
                Directory.CreateDirectory(this.baseDir);
            if (!Directory.Exists(this.packagesDir))
                Directory.CreateDirectory(this.packagesDir);
            if (!Directory.Exists(this.binDir))
                Directory.CreateDirectory(this.binDir);
        }
        public string GetDirectoryForPackage(string packageName, string packageVersion) =>
            Path.Combine(packagesDir, packageName, packageVersion);
        
        public string GetBinFile(string fileName) => Path.Combine(binDir, fileName);
        public string MakeRelativeToBaseDir(string destination)
        {
            if (!destination.StartsWith(baseDir))
                throw new System.ArgumentException(nameof(destination), $"Destination file '{destination}' should start with '{baseDir}'.");
            destination = destination.Substring(baseDir.Length);
            var numberOfDirs = destination.Count(c => c == System.IO.Path.DirectorySeparatorChar);
            return $@"..\{destination}";
        }
    }
}