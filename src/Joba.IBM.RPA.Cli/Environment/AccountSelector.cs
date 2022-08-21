namespace Joba.IBM.RPA.Cli
{
    class AccountSelector
    {
        private readonly IAccountClient client;

        public AccountSelector(IAccountClient client)
        {
            this.client = client;
        }

        public async Task<AccountCredentials> SelectAsync(string? userName, int? tenantCode, CancellationToken cancellation)
        {
            if (string.IsNullOrEmpty(userName))
            {
                Console.Write("User name: ");
                userName = Console.ReadLine() ?? throw new OperationCanceledException("User did not provide the 'user name'");
            }

            if (!tenantCode.HasValue)
                tenantCode = await PromptToSelectTenantAsync(userName, cancellation);

            Console.Write("Password: ");
            var password = ExtendedConsole.Password();

            return new AccountCredentials(tenantCode.Value, userName, password);
        }

        private async Task<int> PromptToSelectTenantAsync(string userName, CancellationToken cancellation)
        {
            var tenants = (await client.FetchTenantsAsync(userName, cancellation)).ToArray();
            if (tenants.Length == 0)
                throw new Exception("Wrong user name or password");

            if (tenants.Length == 1)
                return tenants[0].Code;
            var choice = ExtendedConsole.ShowMenu("Choose a tenant by using the arrow keys to navigate", tenants.Select(t => t.Name).ToArray());
            if (!choice.HasValue)
                throw new Exception("User skipped tenant selection");

            return tenants[choice.Value].Code;
        }
    }
}
