namespace Joba.IBM.RPA.Cli
{
    internal class Constants
    {
        public static string LocalFolder => Path.Combine(System.Environment.ExpandEnvironmentVariables("%localappdata%"), "IBM Robotic Process Automation", "cli");
        public static JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}