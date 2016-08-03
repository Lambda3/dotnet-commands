using DotNetCommands;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace IntegrationTests
{
    [TestClass]
    public class CommandDirectoryTests
    {
        private static CommandDirectory commandDirectory;
        private static string baseDir;

        [ClassInitialize]
#pragma warning disable CC0057 // Unused parameters
        public static void ClassInitialize(TestContext tc)
#pragma warning restore CC0057 // Unused parameters
        {
            baseDir = Path.Combine(Path.GetTempPath(), "foo") + Path.DirectorySeparatorChar;
            commandDirectory = new CommandDirectory(baseDir);
        }

        [TestMethod]
        public void BaseDir() => commandDirectory.BaseDir.Should().Be(baseDir);

        [TestMethod]
        public void BaseDirWithoutDirectorySeparatorCharGetsItAdded() => new CommandDirectory(baseDir.Substring(0, baseDir.Length - 1)).BaseDir.Should().Be(baseDir);

        [TestMethod]
        public void GetBinFile() => commandDirectory.GetBinFile("foo.cmd").Should().Be(Path.Combine(baseDir, "bin", "foo.cmd"));

        [TestMethod]
        public void GetDirectoryForPackage() => commandDirectory.GetDirectoryForPackage("foo", "1.2.3").Should().Be(Path.Combine(baseDir, "packages", "foo", "1.2.3"));

        [TestMethod]
        public void MakeRelativeToBaseDir() => commandDirectory.MakeRelativeToBaseDir(Path.Combine(baseDir, "foo", "bar")).Should().Be(Path.Combine("..", "foo", "bar"));
    }
}
