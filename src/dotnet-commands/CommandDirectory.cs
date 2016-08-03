using System.IO;
using System.Linq;

namespace DotNetCommands
{
    public class CommandDirectory
    {
        public string BaseDir { get; private set; }
        private readonly string packagesDir;
        private readonly string binDir;
        public CommandDirectory(string baseDir)
        {
            BaseDir = baseDir;
            packagesDir = Path.Combine(baseDir, "packages");
            binDir = Path.Combine(baseDir, "bin");
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);
            if (!Directory.Exists(packagesDir))
                Directory.CreateDirectory(packagesDir);
            if (!Directory.Exists(binDir))
                Directory.CreateDirectory(binDir);
        }
        public string GetDirectoryForPackage(string packageName, string packageVersion) =>
            Path.Combine(packagesDir, packageName, packageVersion);

        public string GetBinFile(string fileName) => Path.Combine(binDir, fileName);

        public string MakeRelativeToBaseDir(string destination)
        {
            if (!destination.StartsWith(BaseDir))
                throw new System.ArgumentException(nameof(destination), $"Destination file '{destination}' should start with '{BaseDir}'.");
            destination = destination.Substring(BaseDir.Length);
            var numberOfDirs = destination.Count(c => c == System.IO.Path.DirectorySeparatorChar);
            return $@"..\{destination}";
        }
    }
}