using System;

namespace DotNetCommands
{
    public static class Logger
    {
        private static Action<string> logger = msg => Console.WriteLine(msg);
        public static bool IsVerbose;

        public static void SetLogger(Action<string> logger)
        {
            if (logger == null) throw new ArgumentException("Logger can't be null.", nameof(logger));
            Logger.logger = logger;
        }

#pragma warning disable CC0031
        public static void WriteLineIfVerbose(string msg)
        {
            if (IsVerbose)
                logger(msg);
        }
        public static void WriteLine(string msg) => logger(msg);
#pragma warning restore CC0031
    }
}