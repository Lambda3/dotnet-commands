﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DocoptNet;
using static System.Console;
using static DotNetCommands.Logger;

namespace DotNetCommands
{
    public class Program
    {
        private static string command;
        public static void Main(string[] args)
        {
             const string usage = @"DotNet Commands
  Usage:
    dotnet commands install <command> [--force] [--verbose]
    dotnet commands uninstall <command> [ --verbose]
    dotnet commands update (<command> | all) [--verbose]
    dotnet commands --help
    dotnet commands --version
  Options:
    --force                    Installs even if package was already installed. Optional.
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
            var installer = new Installer(commandDirectory);
            if (arguments["install"].IsTrue)
            {
                var force = arguments["--force"].IsTrue;
                installer.InstallAsync(command, force).Wait();
            }
            if (arguments["uninstall"].IsTrue)
                Uninstall();
            if (arguments["update"].IsTrue)
                Update();
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