using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        }
    }
}