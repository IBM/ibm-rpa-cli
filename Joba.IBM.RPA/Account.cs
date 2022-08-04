using System.CommandLine;
using System.CommandLine.Binding;

namespace Joba.IBM.RPA
{
    record struct Account(int? TenantCode, string? UserName, string? Password = null)
    {
        public async Task<Session> AuthenticateAsync(ApiClient client, CancellationToken cancellation)
        {
            Guid? tenantId = null;
            PromptForUserNameIfEmpty();
            if (!TenantCode.HasValue)
                tenantId = await PromptToSelectTenant(UserName, client, cancellation);
            PromptForPasswordIfEmpty();

            if (tenantId.HasValue)
                return await client.AuthenticateAsync(tenantId.Value, UserName, Password, cancellation);
            if (TenantCode.HasValue)
                return await client.AuthenticateAsync(TenantCode.Value, UserName, Password, cancellation);

            throw new Exception("Could not authenticate because the tenant was not specified");
        }

        private static async Task<Guid?> PromptToSelectTenant(string userName, ApiClient client, CancellationToken cancellation)
        {
            var tenants = (await client.FetchTenantsAsync(userName, cancellation)).ToArray();
            if (tenants.Length == 0)
                throw new Exception("Wrong user name or password");

            if (tenants.Length == 1)
                return tenants[0].Id;
            var choice = ConsoleHelper.ShowMenu("Choose a tenant by using the arrow keys to navigate", tenants.Select(t => t.Name).ToArray());
            if (!choice.HasValue)
                throw new Exception("User skipped tenant selection");

            return tenants[choice.Value].Id;
        }

        private void PromptForPasswordIfEmpty()
        {
            if (string.IsNullOrEmpty(Password))
            {
                Console.Write("Password: ");
                Password = ConsoleHelper.Password();
            }
        }

        private void PromptForUserNameIfEmpty()
        {
            if (string.IsNullOrEmpty(UserName))
            {
                Console.Write("User name: ");
                UserName = Console.ReadLine();
            }
        }
    }

    internal class AccountBinder : BinderBase<Account>
    {
        private readonly Option<int?> tenantOption;
        private readonly Option<string> userNameOption;

        public AccountBinder(Option<int?> tenantOption, Option<string> userNameOption)
        {
            this.tenantOption = tenantOption;
            this.userNameOption = userNameOption;
        }

        protected override Account GetBoundValue(BindingContext bindingContext)
        {
            return new Account(
                bindingContext.ParseResult.GetValueForOption(tenantOption),
                bindingContext.ParseResult.GetValueForOption(userNameOption));
        }
    }
}
