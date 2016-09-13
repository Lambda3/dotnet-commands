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
    dotnet commands install <command>[@<version>] [--force] [--pre] [--verbose]
    dotnet commands uninstall <command> [ --verbose]
    dotnet commands update (<command> | all) [--pre] [--verbose]
    dotnet commands (list|ls) [--verbose]
    dotnet commands --help
    dotnet commands --version

  Options:
    --force                    Installs even if package was already installed. Optional.
    --pre                      Include pre-release versions. Ignored if version is supplied. Optional.
    --verbose                  Verbose. Optional.
    --help -h                  Show this screen.
    --version -v               Show version.

";
            var homeDir = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("userprofile");
            var commandDirectory = new CommandDirectory(Path.Combine(homeDir, ".nuget", "commands"));
            if (args.Length == 1 && args[0] == "bootstrap")
            {
                var installer = new Installer(commandDirectory);
                var success = await installer.InstallAsync("dotnet-commands", null, force: true, includePreRelease: true);
                return (int)(success ? ExitCodes.Success : ExitCodes.BootstrapFailed);
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
                var updateNeeded = await updater.IsUpdateNeededAsync(command, arguments["--pre"].IsTrue);
                var exitCode = updateNeeded == Updater.UpdateNeeded.Yes ? ExitCodes.StartUpdate : ExitCodes.Success;
                WriteLineIfVerbose($"Should update .NET Commands: {updateNeeded}, exit code is going to be {exitCode} ({(int)exitCode}).");
                return (int)exitCode;
            }
            if (arguments["install"].IsTrue)
            {
                var commandParts = command.Split('@');
                NuGet.Versioning.SemanticVersion packageVersion = null;
                switch (commandParts.Length)
                {
                    case 1:
                        break;
                    case 2:
                        command = commandParts[0];
                        try
                        {
                            packageVersion = NuGet.Versioning.SemanticVersion.Parse(commandParts[0]);
                        }
                        catch (ArgumentException)
                        {
                            Console.WriteLine($"Invalid version.\n{usage}");
                            return (int)ExitCodes.InvalidVersion;
                        }
                        break;
                    default:
                        Console.WriteLine($"Invalid version.\n{usage}");
                        return (int)ExitCodes.InvalidVersion;
                }
                var installer = new Installer(commandDirectory);
                var success = await installer.InstallAsync(command, packageVersion, arguments["--force"].IsTrue, arguments["--pre"].IsTrue);
                return (int)(success ? ExitCodes.Success : ExitCodes.InstallFailed);
            }
            if (arguments["uninstall"].IsTrue)
            {
                if (command == "dotnet-commands")
                {
                    WriteLine("Can't uninstall .NET Commands.");
                    return (int)ExitCodes.CantUninstallDotNetCommands;
                }
                var uninstaller = new Uninstaller(commandDirectory);
                var success = await uninstaller.UninstallAsync(command);
                return (int)(success ? ExitCodes.Success : ExitCodes.UninstallFailed);
            }
            if (arguments["update"].IsTrue)
            {
                var updater = new Updater(commandDirectory);
                var updateResult = await updater.UpdateAsync(command, arguments["--force"].IsTrue, arguments["--pre"].IsTrue);
                switch (updateResult)
                {
                    case Updater.UpdateResult.NotNeeded:
                    case Updater.UpdateResult.Success:
                        return (int)ExitCodes.Success;
                    case Updater.UpdateResult.PackageNotFound:
                        return (int)ExitCodes.PackageNotFound;
                    case Updater.UpdateResult.CouldntUninstall:
                        return (int)ExitCodes.CouldntUninstall;
                    case Updater.UpdateResult.UninstalledAndNotReinstalled:
                        return (int)ExitCodes.UninstalledAndNotReinstalled;
                }
            }
            if (arguments["list"].IsTrue || arguments["ls"].IsTrue)
            {
                var lister = new Lister(commandDirectory);
                var success = await lister.ListAsync();
                return (int)(success ? ExitCodes.Success : ExitCodes.ListFailed);
            }
            return (int)ExitCodes.Success;
        }

        enum ExitCodes : byte // 0 and from 64-113 according to http://tldp.org/LDP/abs/html/exitcodes.html
        {
            Success = 0,
            PackageNotFound = 64,
            CouldntUninstall = 65,
            UninstalledAndNotReinstalled = 66,
            ListFailed = 67,
            BootstrapFailed = 68,
            InstallFailed = 69,
            UninstallFailed = 70,
            CantUninstallDotNetCommands = 71,
            InvalidVersion = 72,
            StartUpdate = 113
        }
    }
}