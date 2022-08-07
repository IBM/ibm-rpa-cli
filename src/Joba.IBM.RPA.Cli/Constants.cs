using System.Text.Json;

namespace Joba.IBM.RPA.Cli
{
    internal class Constants
    {
        public static string CliName = "rpa";
        public static string LocalFolder => Path.Combine(System.Environment.ExpandEnvironmentVariables("%localappdata%"), "IBM Robotic Process Automation", "cli");
        public static JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string GetRpaDirectoryPath(string workingDir) => new(Path.Combine(workingDir, ".rpa"));
        public static string GetProjectFilePath(string workingDir) => new(Path.Combine(GetRpaDirectoryPath(workingDir), "project.json"));
    }
}