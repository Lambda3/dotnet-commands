using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static DotNetCommands.Logger;

namespace DotNetCommands
{
    public class PackageInfo
    {
        private readonly string packageDir;

        public PackageInfo(string packageDir)
        {
            this.packageDir = packageDir;
        }

        public async Task<string> GetMainFilePathAsync()
        {
            var commandMetadataTextFilePath = Path.Combine(packageDir, "content", "commandMetadata.json");
            string mainFilePath;
            if (File.Exists(commandMetadataTextFilePath))
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
                    var commandMetadata = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<CommandMetadata>(commandMetadataText));
                    mainFilePath = Path.GetFullPath(Path.Combine(packageDir, commandMetadata.Main));
                }
                catch (Exception ex)
                {
                    WriteLine($"Could not decode Json for '{commandMetadataTextFilePath}'.");
                    WriteLineIfVerbose(ex.ToString());
                    return null;
                }
            }
            else
            {
                var toolsDir = Path.Combine(packageDir, "tools");
                if (!Directory.Exists(toolsDir))
                {
                    WriteLine("This package does not have a tools directory.");
                    return null;
                }
                mainFilePath = Directory.EnumerateFiles(toolsDir, "*.exe")
                    .Union(Directory.EnumerateFiles(toolsDir, "*.cmd"))
                    .Union(Directory.EnumerateFiles(toolsDir, "*.ps1")).FirstOrDefault();
                if (string.IsNullOrEmpty(mainFilePath))
                {
                    WriteLine("This package does not offer any executable.");
                    return null;
                }
            }
            var mainFileName = Path.GetFileName(mainFilePath);
            if (!mainFileName.StartsWith("dotnet-"))
            {
                WriteLine("This package does not offer a .NET CLI extension tool.");
                return null;
            }
            return mainFilePath;
        }

        private class CommandMetadata
        {
            [JsonProperty("main")]
            public string Main { get; set; }
        }
    }
}