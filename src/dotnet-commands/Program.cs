using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DocoptNet;
using static DotNetCommands.Logger;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DotNetCommands
{
    public class Program
    {
        public static int Main(string[] args) => RunAsync(args).Result;

        private async static Task<int> RunAsync(string[] args)
        {
            const string usage = @".NET Commands

  Usage:
    dotnet commands install <command> [--force] [--pre] [--verbose]
    dotnet commands uninstall <command> [ --verbose]
    dotnet commands update (<command> | all) [--pre] [--verbose]
    dotnet commands (list|ls) [--verbose]
    dotnet commands --help
    dotnet commands --version

  Options:
    --force                    Installs even if package was already installed. Optional.
    --pre                      Include pre-release versions. Optional.
    --verbose                  Verbose. Optional.
    --help -h                  Show this screen.
    --version -v               Show version.

";
            var homeDir = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("userprofile");
            var commandDirectory = new CommandDirectory(Path.Combine(homeDir, ".nuget", "commands"));
            if (args.Length == 1 && args[0] == "bootstrap")
            {
                var installer = new Installer(commandDirectory);
                var success = await installer.InstallAsync("dotnet-commands", force: true, includePreRelease: true);
                return success ? 0 : 1;
            }
            var argsWithRun = args;
            if (args.Any() && args[0] != "commands")
                argsWithRun = new[] { "commands" }.Concat(args).ToArray();
            var version = typeof(Program).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            var arguments = new Docopt().Apply(usage, argsWithRun, version: version, exit: true);
            var verbose = arguments["--verbose"].IsTrue;
            Logger.IsVerbose = verbose;
            var command = arguments["<command>"]?.ToString();
            if (IsVerbose)
            {
                WriteLine($".NET Commands running on {RuntimeInformation.OSDescription} on {RuntimeInformation.ProcessArchitecture} (system is {RuntimeInformation.OSArchitecture}) with framework {RuntimeInformation.FrameworkDescription}.");
                WriteLine($"Args: {string.Join(" ", args.Select(s => $"\"{s}\""))}");
            }

            if (command == "dotnet-commands" && (arguments["install"].IsTrue || arguments["update"].IsTrue))
            {
                WriteLineIfVerbose("Request to update .NET Commands.");
                var updater = new Updater(commandDirectory);
                var shouldntUpdate = await updater.ShouldntUpdateAsync(command, arguments["--pre"].IsTrue);
                var exitCode = shouldntUpdate ? 0 : 200;
                WriteLineIfVerbose($"Should update .NET Commands: {!shouldntUpdate}, exit code is going to be {exitCode}.");
                return exitCode;
            }
            if (arguments["install"].IsTrue)
            {

                var installer = new Installer(commandDirectory);
                var success = await installer.InstallAsync(command, arguments["--force"].IsTrue, arguments["--pre"].IsTrue);
                return success ? 0 : 1;
            }
            if (arguments["uninstall"].IsTrue)
            {
                if (command == "dotnet-commands")
                {
                    WriteLine("Can't uninstall .NET Commands.");
                    return 1;
                }
                var uninstaller = new Uninstaller(commandDirectory);
                var success = await uninstaller.UninstallAsync(command);
                return success ? 0 : 1;
            }
            if (arguments["update"].IsTrue)
            {
                var updater = new Updater(commandDirectory);
                var success = await updater.UpdateAsync(command, arguments["--force"].IsTrue, arguments["--pre"].IsTrue);
                return success ? 0 : 1;
            }
            if (arguments["list"].IsTrue || arguments["ls"].IsTrue)
            {
                var lister = new Lister(commandDirectory);
                var success = await lister.ListAsync();
                return success ? 0 : 1;
            }
            return 0;
        }
    }
}