using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using static DotNetCommands.Logger;

[assembly: LevelOfParallelism(1)]
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
            WriteLineIfVerbose($".NET Commands running on {RuntimeInformation.OSDescription} on {RuntimeInformation.ProcessArchitecture} (system is {RuntimeInformation.OSArchitecture}) with framework {RuntimeInformation.FrameworkDescription}.");
        }
    }
}