using DotNetCommands;
using FluentAssertions;
using NuGet.Versioning;
using NUnit.Framework;
using System.IO;

namespace IntegrationTests
{
    [TestFixture]
    public class CommandDirectoryTests
    {
        private CommandDirectory commandDirectory;
        private string baseDir;

        [OneTimeSetUp]
        public void ClassInitialize()
        {
            baseDir = Path.Combine(Path.GetTempPath(), "foo") + Path.DirectorySeparatorChar;
            commandDirectory = new CommandDirectory(baseDir);
        }

        [Test]
        public void BaseDir() => commandDirectory.BaseDir.Should().Be(baseDir);

        [Test]
        public void BaseDirWithoutDirectorySeparatorCharGetsItAdded() => new CommandDirectory(baseDir.Substring(0, baseDir.Length - 1)).BaseDir.Should().Be(baseDir);

        [Test]
        public void GetBinFile() => commandDirectory.GetBinFile("foo.cmd").Should().Be(Path.Combine(baseDir, "bin", "foo.cmd"));

        [Test]
        public void GetDirectoryForPackage() => commandDirectory.GetDirectoryForPackage("foo", SemanticVersion.Parse("1.2.3")).Should().Be(Path.Combine(baseDir, "packages", "foo", "1.2.3"));

        [Test]
        public void MakeRelativeToBaseDir() => commandDirectory.MakeRelativeToBaseDir(Path.Combine(baseDir, "foo", "bar")).Should().Be(Path.Combine("..", "foo", "bar"));
    }
}
