namespace Joba.IBM.RPA.Cli
{
    class AccountSelector
    {
        private readonly IConsole console;
        private readonly IAccountResource resource;

        public AccountSelector(IConsole console, IAccountResource resource)
        {
            this.console = console;
            this.resource = resource;
        }

        public async Task<AccountCredentials> SelectAsync(string? userName, int? tenantCode, string? password, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(userName))
            {
                console.Write("User name: ");
                userName = Console.ReadLine() ?? throw new OperationCanceledException("User did not provide the 'user name'");
            }

            if (!tenantCode.HasValue)
                tenantCode = await PromptToSelectTenantAsync(userName, cancellation);

            if (string.IsNullOrEmpty(password))
            {
                console.Write("Password: ");
                password = console.Password();
            }

            return new AccountCredentials(tenantCode.Value, userName, password);
        }

        private async Task<int> PromptToSelectTenantAsync(string userName, CancellationToken cancellation)
        {
            var tenants = (await resource.FetchTenantsAsync(userName, cancellation)).ToArray();
            if (tenants.Length == 0)
                throw new Exception("Wrong user name or password");

            if (tenants.Length == 1)
                return tenants[0].Code;
            var choice = console.ShowMenu("Choose a tenant by using the arrow keys to navigate", tenants.Select(t => t.Name).ToArray());
            if (!choice.HasValue)
                throw new Exception("User skipped tenant selection");

            return tenants[choice.Value].Code;
        }
    }
}
