namespace Joba.IBM.RPA
{
    public class ProjectException : Exception
    {
        public ProjectException(string message)
            : base(message) { }

        public ProjectException(string message, Exception innerException)
        : base(message, innerException) { }

        public static ProjectException ThrowRequired(string commandLine) =>
            new($"No project is initialized. The command '{commandLine}' requires a project.");
    }
}
