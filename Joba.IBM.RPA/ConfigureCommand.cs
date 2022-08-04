using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Text.Json;

namespace Joba.IBM.RPA
{
    class ConfigureCommand : Command
    {
        private static readonly string CommandName = "configure";

        public ConfigureCommand() : base(CommandName, "Configures the account to connect to RPA the RPA Command Line Interface (CLI)")
        {
            var region = new Option<string>("--region", "The region you want to connect to") { IsRequired = true };
            var userName = new Option<string>("--userName", "Your user name") { IsRequired = true };
            var tenant = new Option<int>("--tenant", "The tenant code") { IsRequired = true };
            var password = new Option<string>("--password", "Your password") { IsRequired = true };

            AddOption(region);
            AddOption(userName);
            AddOption(tenant);
            AddOption(password);
            this.SetHandler(Handle, region, new AccountBinder(tenant, userName, password),
                Bind.FromServiceProvider<ServerConfig>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task Handle(string region, Account account, ServerConfig config, InvocationContext context)
        {
            var console = context.Console;
            var cancellation = context.GetCancellationToken();

            using var client = config.CreateClient(region);
            var session = await client.AuthenticateAsync(account, cancellation);
            var profile = Profile.Create(account, session);
            using var stream = File.OpenWrite(Constants.ProfileFilePath);
            await JsonSerializer.SerializeAsync(stream, profile, Constants.SerializerOptions);

            console.WriteLine($"Hi {profile.PersonName}, the CLI has been configured:");
            console.WriteLine($"Tenant: {profile.TenantCode} - {profile.TenantName}");
            console.WriteLine($"Use the '{Constants.CliName} {CommandName}' command again to overwrite this configuration");
        }
    }
}