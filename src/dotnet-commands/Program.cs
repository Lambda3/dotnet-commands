using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DocoptNet;
using static DotNetCommands.Logger;

namespace DotNetCommands
{
    public class Program
    {
        private static string command;
        public static int Main(string[] args)
        {
            const string usage = @"DotNet Commands
  Usage:
    dotnet commands install <command> [--force] [--pre] [--verbose]
    dotnet commands uninstall <command> [ --verbose]
    dotnet commands update (<command> | all) [--pre] [--verbose]
    dotnet commands --help
    dotnet commands --version
  Options:
    --force                    Installs even if package was already installed. Optional.
    --pre                      Include pre-release versions. Optional.
    --verbose                  Verbose. Optional.
    --help -h                  Show this screen.
    --version -v               Show version.
";
            var argsWithRun = args;
            if (args.Any() && args[0] != "commands")
                argsWithRun = new[] { "commands" }.Concat(args).ToArray();
            var arguments = new Docopt().Apply(usage, argsWithRun, version: Assembly.GetEntryAssembly().GetName().Version, exit: true);
            var verbose = arguments["--verbose"].IsTrue;
            var pre = arguments["--pre"].IsTrue;
            Logger.IsVerbose = verbose;
            command = arguments["<command>"].ToString();
            var homeDir = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("userprofile");
            var commandDirectory = new CommandDirectory(Path.Combine(homeDir, ".nuget", "commands"));
            var installer = new Installer(commandDirectory);
            if (arguments["install"].IsTrue)
            {
                var force = arguments["--force"].IsTrue;
                var success = installer.InstallAsync(command, force, pre).Result;
                return success ? 0 : 1;
            }
            if (arguments["uninstall"].IsTrue)
                Uninstall();
            if (arguments["update"].IsTrue)
                Update();
            return 0;
        }

        public static void Uninstall()
        {
            WriteLine("Not implemented yet.");
        }

        public static void Update()
        {
            WriteLine("Not implemented yet.");
        }
    }
}