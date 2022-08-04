using System.Text.Json;

namespace Joba.IBM.RPA
{
    internal class Constants
    {
        public static string CliName = "rpa";
        public static string LocalFolder => Path.Combine(Environment.ExpandEnvironmentVariables("%localappdata%"), "IBM Robotic Process Automation", "cli");
        public static string ServerFilePath => Path.Combine(LocalFolder, "server.json");
        public static string ProfileFilePath => Path.Combine(LocalFolder, $"{Environment.UserName}.json");
        public static JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}