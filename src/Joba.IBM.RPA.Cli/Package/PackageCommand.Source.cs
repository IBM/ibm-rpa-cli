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
                var password = new Option<string?>("--password", "The user password.");
                var properties = new Option<IEnumerable<string>?>(new[] { "--property", "-p" }, $"A key-value pair property used by '{RpaCommand.ServiceName}' extensions. For example, to pass the CloudPak Console Url: -p:{PropertyOptions.CloudPakConsoleAddress}=[url].") { AllowMultipleArgumentsPerToken = true };

                AddArgument(alias);
                AddOption(url);
                AddOption(region);
                AddOption(userName);
                AddOption(tenant);
                AddOption(password);
                this.SetHandler(HandleAsync,
                    new RemoteOptionsBinder(alias, url, region, userName, tenant, password),
                    new PropertyOptionsBinder(properties),
                    Bind.FromLogger<PackageSourceCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<ISecretProvider>(),
                    Bind.FromServiceProvider<IAccountAuthenticatorFactory>(),
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(RemoteOptions options, PropertyOptions properties, ILogger<PackageSourceCommand> logger, IRpaClientFactory clientFactory,
                ISecretProvider secretProvider, IAccountAuthenticatorFactory authenticatorFactory, IProject project, InvocationContext context)
            {
                var handler = new AddPackageSourceHandler(logger, project, context.Console, clientFactory, secretProvider, authenticatorFactory);
                await handler.HandleAsync(options, properties, context.GetCancellationToken());
            }
        }

        internal class AddPackageSourceHandler
        {
            private readonly ILogger logger;
            private readonly IConsole console;
            private readonly IProject project;
            private readonly IRpaClientFactory clientFactory;
            private readonly ISecretProvider secretProvider;
            private readonly IAccountAuthenticatorFactory authenticatorFactory;

            public AddPackageSourceHandler(ILogger logger, IProject project, IConsole console, IRpaClientFactory clientFactory,
                ISecretProvider secretProvider, IAccountAuthenticatorFactory authenticatorFactory)
            {
                this.logger = logger;
                this.console = console;
                this.project = project;
                this.clientFactory = clientFactory;
                this.secretProvider = secretProvider;
                this.authenticatorFactory = authenticatorFactory;
            }

            internal async Task HandleAsync(RemoteOptions options, PropertyOptions properties, CancellationToken cancellation)
            {
                project.EnsureCanConfigure(options.Alias);
                var password = secretProvider.GetSecret(options);
                var serverSelector = new ServerSelector(RpaCommand.SupportedServerVersion, console, clientFactory, project);
                var server = await serverSelector.SelectAsync(options.Address, cancellation);
                var regionSelector = new RegionSelector(console);
                var region = regionSelector.Select(server, options.RegionName);

                using var client = clientFactory.CreateFromRegion(region);
                var authenticator = authenticatorFactory.Create(server.Deployment, server.AuthenticationMethod, region, properties);
                var accountSelector = new AccountSelector(console, client.Account);
                var credentials = await accountSelector.SelectAsync(options.UserName, options.TenantCode, password, cancellation);
                var session = await authenticator.AuthenticateAsync(credentials, cancellation);

                var package = project.PackageSources.Add(options.Alias, region, session, server, properties);
                await project.SaveAsync(cancellation);

                logger.LogInformation("Package source added: {Package}", package);
            }
        }
    }
}