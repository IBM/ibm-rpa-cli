﻿using Microsoft.Extensions.Logging;
using Polly;

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
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(RemoteOptions options, ILogger<PackageSourceCommand> logger, IRpaClientFactory clientFactory,
                IProject project, InvocationContext context)
            {
                var handler = new AddPackageSourceHandler(logger, project, context.Console, clientFactory);
                await handler.HandleAsync(options, context.GetCancellationToken());
            }
        }

        internal class AddPackageSourceHandler
        {
            private readonly ILogger logger;
            private readonly IConsole console;
            private readonly IProject project;
            private readonly IRpaClientFactory clientFactory;

            public AddPackageSourceHandler(ILogger logger, IProject project, IConsole console, IRpaClientFactory clientFactory)
            {
                this.logger = logger;
                this.console = console;
                this.project = project;
                this.clientFactory = clientFactory;
            }

            internal async Task HandleAsync(RemoteOptions options, CancellationToken cancellation)
            {
                project.EnsureCanConfigure(options.Alias);
                var regionSelector = new RegionSelector(console, clientFactory, project);
                var region = await regionSelector.SelectAsync(options.Address, options.RegionName, cancellation);

                using var client = clientFactory.CreateFromRegion(region);
                var accountSelector = new AccountSelector(console, client.Account);
                var credentials = await accountSelector.SelectAsync(options.UserName, options.TenantCode, options.Password, cancellation);

                var package = await project.PackageSources.AddAsync(client.Account, options.Alias, region, credentials, cancellation);
                await project.SaveAsync(cancellation);

                logger.LogInformation("Package source added: {Package}", package);
            }
        }
    }
}