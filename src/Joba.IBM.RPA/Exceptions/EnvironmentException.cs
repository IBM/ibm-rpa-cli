namespace Joba.IBM.RPA
{
    public class EnvironmentException : Exception
    {
        public EnvironmentException(string message)
            : base(message) { }

        public EnvironmentException(string message, Exception innerException)
        : base(message, innerException) { }

        public static EnvironmentException NoEnvironment(string commandName) =>
            new($"No environment is loaded. The command '{commandName}' requires an environment.");
    }
}
