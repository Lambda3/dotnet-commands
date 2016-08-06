using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static DotNetCommands.Logger;

namespace DotNetCommands
{
    public class PackageInfo
    {
        private PackageInfo() { }
        public string PackageDir { get; private set; }
        public IList<PackageCommand> Commands { get; set; }

        //todo: refactor. Desperately. This is too long, and is doing too much. So I added lots of comments. For now, it is good enough, as we have tests! :)
        public static async Task<PackageInfo> GetMainFilePathAsync(string packageName, string packageDir)
        {
            var commandMetadataTextFilePath = Path.Combine(packageDir, "content", "commandMetadata.json");
            var commands = new List<PackageCommand>();
            if (File.Exists(commandMetadataTextFilePath)) //if we have a command metadata file, use it
            {
                string commandMetadataText;
                try
                {
                    commandMetadataText = File.ReadAllText(commandMetadataTextFilePath);
                }
                catch (Exception ex)
                {
                    WriteLine($"Could not read command metadata file '{commandMetadataTextFilePath}'.");
                    WriteLineIfVerbose(ex.ToString());
                    return null;
                }
                try
                {
                    dynamic commandMetadata = await Task.Factory.StartNew(() => JObject.Parse(commandMetadataText));
                    //command metadata can have a main value, or can have commands, here we take care of this difference
                    //first for main
                    if (commandMetadata.main != null)
                    {
                        if (!packageName.StartsWith("dotnet-"))
                        {
                            WriteLine($"If using 'main', the package name must start with dotnet-.");
                            return null;
                        }
                        commands.Add(new PackageCommand
                        {
                            ExecutableFilePath = (string)commandMetadata.main,
                            Name = packageName
                        });
                    }
                    //then for commands
                    else if (commandMetadata.commands != null)
                    {
                        foreach (JProperty command in commandMetadata.commands)
                        {
                            var commandName = command.Name;
                            if (!commandName.StartsWith("dotnet-"))
                            {
                                WriteLine($"Found command with name that does not start with dotnet-. Skipping.");
                                continue;
                            }
                            if (command.Value.Type != JTokenType.String)
                            {
                                WriteLine($"JSON for metadata on file '{commandMetadataTextFilePath}' is not in a suitable format.");
                                return null;
                            }
                            commands.Add(new PackageCommand
                            {
                                ExecutableFilePath = (string)command.Value,
                                Name = commandName
                            });
                        }
                    }
                    else
                    {
                        WriteLine($"JSON for metadata on file '{commandMetadataTextFilePath}' is not in a suitable format.");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    WriteLine($"Could not decode Json for '{commandMetadataTextFilePath}'.");
                    WriteLineIfVerbose(ex.ToString());
                    return null;
                }
                //at the end we normalize the extensions for each command
                //the command author should supply both the extension (Windows) and the non extension (Linux) files
                foreach (var command in commands)
                {
                    var extension = Path.GetExtension(command.ExecutableFilePath);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (string.IsNullOrWhiteSpace(extension))
                        {
                            //if the command author did not add the .cmd extension to the executable, and we are on windows, we do it here
                            command.ExecutableFilePath = $"{command.ExecutableFilePath}.cmd";
                        }
                    }
                    else
                    {
                        //if the command author did add the .cmd, .ps1 or .exe extension to the executable, and we are on Linux or Mac (not Windows), we remove the extension
                        if (extension.Equals(".exe", StringComparison.CurrentCultureIgnoreCase)
                            || extension.Equals(".cmd", StringComparison.CurrentCultureIgnoreCase)
                            || extension.Equals(".ps1", StringComparison.CurrentCultureIgnoreCase))
                        {
                            command.ExecutableFilePath = Path.Combine(Path.GetDirectoryName(command.ExecutableFilePath),
                                Path.GetFileNameWithoutExtension(command.ExecutableFilePath));
                        }
                    }
                }
            }
            else
            {//here the command author did not specify a command metadata file, so we check directly the tools dir
                var toolsDir = Path.Combine(packageDir, "tools");
                if (!Directory.Exists(toolsDir))
                {
                    WriteLine("This package does not have a tools directory.");
                    return null;
                }
                //we search for all files, according to platform
                var files = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Directory.EnumerateFiles(toolsDir, "*.exe")
                        .Union(Directory.EnumerateFiles(toolsDir, "*.cmd"))
                        .Union(Directory.EnumerateFiles(toolsDir, "*.ps1")).ToList()
                    : Directory.EnumerateFiles(toolsDir, "*.").ToList();
                switch (files.Count)
                {
                    case 0:// if we can't find any file, we have a problem
                        {
                            WriteLine("This package does not offer any executable.");
                            return null;
                        }
                    case 1://if we only have one file, than we have a special case, we can use the command name if it starts with dotnet-*
                        {
                            var fullFilePath = files.First();
                            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullFilePath);
                            if (!fileNameWithoutExtension.StartsWith("dotnet-") && !packageName.StartsWith("dotnet-"))
                            {//if neither the command nor the package start with dotnet-, then we have a problem
                                WriteLine("This package does not offer any way to be linked to .NET CLI, it needs to either be named dotnet-* or offer a tool with such a name.");
                                return null;
                            }
                            //if the tool name starts with dotnet-, we use it, otherwise we use the package name.
                            //tool name is getting preference. is it correct?
                            var commandName = fileNameWithoutExtension.StartsWith("dotnet-")
                                ? fileNameWithoutExtension
                                : packageName;
                            commands.Add(new PackageCommand
                            {
                                ExecutableFilePath = fullFilePath,
                                Name = commandName
                            });
                            break;
                        }
                    default:
                        {//here we have more than one tool, we can only take the ones that start with dotnet-* as we don't have a metadata file
                            foreach (var fullFilePath in files)
                            {
                                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullFilePath);
                                var commandName = fileNameWithoutExtension.StartsWith("dotnet-")
                                    ? fileNameWithoutExtension
                                    : packageName;
                                commands.Add(new PackageCommand
                                {
                                    ExecutableFilePath = fullFilePath,
                                    Name = commandName
                                });
                            }
                            commands = commands.Distinct().ToList();//we need distinct because we could have
                            //more than one tool that does not start with dotnet-* and the package could be names dotnet-*,
                            //then we would end up several tools with the same name
                            break;
                        }
                }
            }
            if (!commands.Any())
            {
                WriteLine("This package does not offer any way to be linked to .NET CLI, it needs to either be named dotnet-* or offer a tool with such a name.");
                return null;
            }
            foreach (var command in commands)//here we add the full path to all commands
                command.ExecutableFilePath = Path.GetFullPath(Path.Combine(packageDir, command.ExecutableFilePath));
            var pi = new PackageInfo
            {
                PackageDir = packageDir,
                Commands = commands
            };
            return pi;
        }

        private class CommandMetadataWithMain
        {
            [JsonProperty("main")]
            public string Main { get; set; }
        }
    }
}