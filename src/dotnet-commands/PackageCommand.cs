using System;

namespace DotNetCommands
{
    public class PackageCommand : IComparable<PackageCommand>
    {
        public string Name { get; set; }
        public string ExecutableFilePath { get; set; }

        int IComparable<PackageCommand>.CompareTo(PackageCommand other) =>
            string.Compare(Name, other.Name);
    }
}