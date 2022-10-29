using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject]
        internal class PackageSourceCommand : Command
        {
            public const string CommandName = "source";
            public PackageSourceCommand() : base(CommandName, "Adds a package source")
            {
                var alias = new Argument<string>("alias", "The source name");
                var url = new Option<string?>("--url", $"The server domain url. You can specify '{ServerAddress.DefaultOptionName}' to use {ServerAddress.DefaultUrl}");
                var region = new Option<string?>("--region", "The region of the package source");
                var userName = new Option<string?>("--userName", "The user name to authenticate, usually the e-mail, to use for authentication");
                var tenant = new Option<int?>("--tenant", "The tenant code to use for authentication");
                var password = new Option<string?>("--password", "The user password.") { IsHidden = true };

                AddArgument(alias);
                AddOption(url);
                AddOption(region);
                AddOption(userName);
                AddOption(tenant);
                AddOption(password);
                this.SetHandler(HandleAsync,
                    new RemoteOptionsBinder(alias, url, region, userName, tenant, password),
                    Bind.FromLogger<PackageSourceCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(RemoteOptions options, ILogger<PackageSourceCommand> logger, IRpaClientFactory clientFactory,
                Project project, InvocationContext context)
            {
                project.EnsureCanConfigure(options.Alias);
                var cancellation = context.GetCancellationToken();
                var regionSelector = new RegionSelector(context.Console, clientFactory, project);
                var region = await regionSelector.SelectAsync(options.Address, options.RegionName, cancellation);

                using var client = clientFactory.CreateFromRegion(region);
                var accountSelector = new AccountSelector(context.Console, client.Account);
                var credentials = await accountSelector.SelectAsync(options.UserName, options.TenantCode, options.Password, cancellation);

                var package = await project.PackageSources.AddAsync(client.Account, options.Alias, region, credentials, cancellation);
                await project.SaveAsync(cancellation);

                logger.LogInformation("Package source added: {Package}", package);
            }
        }
    }
}