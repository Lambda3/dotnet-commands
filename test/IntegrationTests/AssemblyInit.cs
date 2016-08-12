using NUnit.Framework;
using System;

namespace IntegrationTests
{
    [SetUpFixture]
    public class AssemblyInit
    {
        [OneTimeSetUp]
        public static void AssemblyInitialize()
        {
            DotNetCommands.Logger.IsVerbose = true;
            DotNetCommands.Logger.SetLogger(msg => Console.WriteLine($"[Tool {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}] {msg}"));
        }
    }
}