namespace Joba.IBM.RPA
{
    internal class Constants
    {
        public static string CliName = "rpa";
        public static string LocalFolder => Path.Combine(System.Environment.ExpandEnvironmentVariables("%localappdata%"), "IBM Robotic Process Automation", "cli");
        public static string GetRpaDirectoryPath(string workingDir) => new(Path.Combine(workingDir, ".rpa"));
        //public static string GetProjectFilePath(string workingDir) => new(Path.Combine(GetRpaDirectoryPath(workingDir), "project.json"));
    }
}