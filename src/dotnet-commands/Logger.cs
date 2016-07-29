using static System.Console;

namespace DotNetCommands
{
    public static class Logger
    {
        public static bool IsVerbose;
        public static void WriteLineIfVerbose(string msg)
        {
            if (IsVerbose)
                WriteLine(msg);
        }
    }
}