using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace Joba.IBM.RPA
{
    class ConfigureCommand : Command
    {
        public static readonly string CommandName = "configure";

        public ConfigureCommand() : base(CommandName, 
            $"Configures which region and account to connect. This will create a profile using your current windows session named '{Environment.UserName}'")
        {
            var region = new Option<string>("--region", "The region you want to connect");
            var userName = new Option<string>("--userName", "The User Name, usually the e-mail, to use for authentication");
            var tenant = new Option<int?>("--tenant", "The Tenant Code to use for authentication");

            AddOption(region);
            AddOption(userName);
            AddOption(tenant);
            this.SetHandler(Handle, region, userName, tenant,
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task Handle(string? regionName, string? userName, int? tenantCode, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            using var regionSelector = new RegionSelector();
            var region = await regionSelector.SelectAsync(regionName, cancellation);

            using var client = region.CreateClient();
            var accountSelector = new AccountSelector(client.Account);
            var account = await accountSelector.SelectAsync(userName, tenantCode, cancellation);
            var session = await account.AuthenticateAsync(client.Account, cancellation);
            
            var profile = Profile.Create(region, account, session);
            await profile.SaveAsync(cancellation);

            ExtendedConsole.WriteLine($"Hi {profile.PersonName:blue}, the CLI has been configured:");
            ExtendedConsole.WriteLine($"Region {profile.RegionName:blue}, Tenant {profile.TenantCode:blue} - {profile.TenantName:blue}");
            ExtendedConsole.WriteLine($"Use the {Constants.CliName:blue} {Name:blue} command again to overwrite this configuration");
        }
    }
}