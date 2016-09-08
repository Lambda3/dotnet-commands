using DotNetCommands;
using System;
using System.Diagnostics;
using System.IO;

namespace IntegrationTests
{
    public sealed class CommandDirectoryCleanup : IDisposable
    {
        public CommandDirectory CommandDirectory { get; private set; }

        public CommandDirectoryCleanup()
        {
            CommandDirectory = new CommandDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10)));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(CommandDirectory.BaseDir, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not delete the base dir '{CommandDirectory.BaseDir}'.\n{ex.ToString()}");
            }
        }
    }
}