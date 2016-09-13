using NuGet.Versioning;
using System.IO;

namespace DotNetCommands
{
    public class CommandDirectory
    {
        public string BaseDir { get; private set; }
        public string PackagesDir { get; private set; }
        private readonly string binDir;
        public CommandDirectory(string baseDir)
        {
            if (string.IsNullOrWhiteSpace(baseDir)) throw new System.ArgumentException("You have to supply the base dir.", nameof(baseDir));
            BaseDir = !baseDir.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? baseDir + Path.DirectorySeparatorChar
                : baseDir;
            PackagesDir = Path.Combine(baseDir, "packages");
            binDir = Path.Combine(baseDir, "bin");
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);
            if (!Directory.Exists(PackagesDir))
                Directory.CreateDirectory(PackagesDir);
            if (!Directory.Exists(binDir))
                Directory.CreateDirectory(binDir);
        }

        public string GetDirectoryForPackage(string packageName, SemanticVersion packageVersion = null) =>
            packageVersion == null
            ? Path.Combine(PackagesDir, packageName)
            : Path.Combine(PackagesDir, packageName, packageVersion.ToString());

        public string GetBinFile(string fileName) =>
            Path.Combine(binDir, fileName.Replace('/', Path.DirectorySeparatorChar));

        public string MakeRelativeToBaseDir(string destination)
        {
            if (!destination.StartsWith(BaseDir))
                throw new System.ArgumentException(nameof(destination), $"Destination file '{destination}' should start with '{BaseDir}'.");
            return ".." + Path.DirectorySeparatorChar + destination.Substring(BaseDir.Length);
        }
    }
}