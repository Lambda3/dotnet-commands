using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DocoptNet;

namespace DotNetCommands
{
    public class Program
    {
        private static string command;
        public static int Main(string[] args)
        {
            const string usage = @".NET Commands

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
            Logger.IsVerbose = verbose;
            command = arguments["<command>"].ToString();
            var homeDir = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("userprofile");
            var commandDirectory = new CommandDirectory(Path.Combine(homeDir, ".nuget", "commands"));
            if (arguments["install"].IsTrue)
            {
                var installer = new Installer(commandDirectory);
                var success = installer.InstallAsync(command, arguments["--force"].IsTrue, arguments["--pre"].IsTrue).Result;
                return success ? 0 : 1;
            }
            if (arguments["uninstall"].IsTrue)
            {
                var uninstaller = new Uninstaller(commandDirectory);
                var success = uninstaller.UninstallAsync(command).Result;
                return success ? 0 : 1;
            }
            if (arguments["update"].IsTrue)
            {
                var updater = new Updater(commandDirectory);
                var success = updater.UpdateAsync(command, arguments["--force"].IsTrue, arguments["--pre"].IsTrue).Result;
                return success ? 0 : 1;
            }
            return 0;
        }
    }
}