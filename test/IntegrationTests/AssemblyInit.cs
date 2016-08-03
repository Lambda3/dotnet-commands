using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IntegrationTests
{
    [TestClass]
    public class AssemblyInit
    {
        [AssemblyInitialize]
#pragma warning disable CC0057 // Unused parameters
        public static void AssemblyInitialize(TestContext tc)
#pragma warning restore CC0057 // Unused parameters
        {
            DotNetCommands.Logger.IsVerbose = true;
            DotNetCommands.Logger.SetLogger(msg => Console.WriteLine($"[Tool {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}] {msg}"));
        }
    }
}