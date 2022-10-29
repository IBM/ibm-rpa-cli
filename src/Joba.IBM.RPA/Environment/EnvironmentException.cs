namespace Joba.IBM.RPA
{
    public class EnvironmentException : Exception
    {
        public EnvironmentException(string alias, string message)
            : base(message)
        {
            Alias = alias;
        }

        public EnvironmentException(string alias, string message, Exception innerException)
        : base(message, innerException)
        {
            Alias = alias;
        }

        public static EnvironmentException NotConfigured(string alias) =>
            new(alias, $"The environment alias {alias} is not configured.");

        public string Alias { get; }
    }
}
