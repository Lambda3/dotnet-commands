using System.IO;

namespace DotNetCommands
{
    public class CommandDirectory
    {
        public string BaseDir { get; private set; }
        private readonly string packagesDir;
        private readonly string binDir;
        public CommandDirectory(string baseDir)
        {
            if (string.IsNullOrWhiteSpace(baseDir)) throw new System.ArgumentException("You have to supply the base dir.", nameof(baseDir));
            BaseDir = !baseDir.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? baseDir + Path.DirectorySeparatorChar
                : baseDir;
            packagesDir = Path.Combine(baseDir, "packages");
            binDir = Path.Combine(baseDir, "bin");
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);
            if (!Directory.Exists(packagesDir))
                Directory.CreateDirectory(packagesDir);
            if (!Directory.Exists(binDir))
                Directory.CreateDirectory(binDir);
        }

        public string GetDirectoryForPackage(string packageName, string packageVersion = null) =>
            packageVersion == null
            ? Path.Combine(packagesDir, packageName)
            : Path.Combine(packagesDir, packageName, packageVersion);

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