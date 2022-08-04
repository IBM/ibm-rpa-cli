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
            var region = new Option<string>("--region", "The region you want to connect") { IsRequired = true };
            var userName = new Option<string>("--userName", "The User Name, usually the e-mail, to use for authentication");
            var tenant = new Option<int?>("--tenant", "The Tenant Code to use for authentication");

            AddOption(region);
            AddOption(userName);
            AddOption(tenant);
            this.SetHandler(Handle, region, new AccountBinder(tenant, userName),
                Bind.FromServiceProvider<ServerConfig>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task Handle(string region, Account account, ServerConfig config, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();

            using var client = config.CreateClient(region);
            var session = await account.AuthenticateAsync(client, cancellation);
            var profile = Profile.Create(account, session);
            using var stream = File.OpenWrite(Constants.ProfileFilePath);
            await JsonSerializer.SerializeAsync(stream, profile, Constants.SerializerOptions);

            Console.WriteLine($"Hi {profile.PersonName}, the CLI has been configured:");
            Console.WriteLine($"Tenant: {profile.TenantCode} - {profile.TenantName}");
            Console.WriteLine($"Use the '{Constants.CliName} {CommandName}' command again to overwrite this configuration");
        }
    }
}