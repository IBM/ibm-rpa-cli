namespace Joba.IBM.RPA.Cli
{
    class DefaultSecretProvider : ISecretProvider
    {
        private static readonly string[] EnvironmentKeys = new string[] {
            "TF_BUILD",
            "BUILDKITE",
            "CIRCLECI",
            "CIRRUS_CI",
            "CODEBUILD_BUILD_ID",
            "GITHUB_ACTIONS",
            "GITLAB_CI",
            "HEROKU_TEST_RUN_ID",
            "BUILD_ID",
            "TEAMCITY_VERSION",
            "TRAVIS"
        };

        string? ISecretProvider.GetSecret(RemoteOptions options)
        {
            if (options.Password is not null)
                return options.Password;

            var key = $"RPA_SECRET_{options.Alias}";
            var password = System.Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(password) && IsRunningOnContinuousIntegration())
                throw new SecretNotFoundException($"Running on CI systems requires the password to be provided as an environment variable. Set the following environment variable '{key}'");

            return password;
        }

        private static bool IsRunningOnContinuousIntegration()
        {
            foreach (var key in EnvironmentKeys)
                if (System.Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process) is not null)
                    return true;

            return false;
        }
    }

    interface ISecretProvider
    {
        string? GetSecret(RemoteOptions options);
    }

    class SecretNotFoundException : Exception
    {
        public SecretNotFoundException(string message)
            : base(message) { }

        public SecretNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
    }
}