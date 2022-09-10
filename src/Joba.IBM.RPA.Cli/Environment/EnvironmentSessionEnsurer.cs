namespace Joba.IBM.RPA.Cli
{
    class EnvironmentSessionEnsurer
    {
        private readonly IRpaClient client;
        private readonly Environment environment;
        private readonly ShallowEnvironmentRenderer renderer;

        public EnvironmentSessionEnsurer(IRpaClient client, Environment environment)
        {
            this.client = client;
            this.environment = environment;
            renderer = new ShallowEnvironmentRenderer();
        }

        public async Task<Session> EnsureAsync(CancellationToken cancellation)
        {
            if (environment.Session.Current == Session.NoSession)
            {
                ExtendedConsole.WriteLine($"You need to provide credentials for the environment:");
                renderer.RenderLineIndented(environment, 2);
                Console.Write("User name: ");
                var userName = Console.ReadLine() ?? throw new OperationCanceledException("User did not provide the 'user name'");
                Console.Write("Password: ");
                var password = ExtendedConsole.Password();
                return await environment.Session.CreateAndSaveAsync(client.Account, userName, password, cancellation);
            }

            if (environment.Session.Current.IsExpired)
            {
                ExtendedConsole.WriteLine($"Your session is expired. " +
                    $"The {RpaCommand.CommandName} tool will try to log in again using tenant={environment.Session.Current.TenantName:blue} " +
                    $"and user={environment.Session.Current.UserName}. Please provide the password:");
                var password = ExtendedConsole.Password();
                return await environment.Session.RenewAndSaveAsync(client.Account, password, cancellation);
            }

            return environment.Session.Current;
        }
    }
}