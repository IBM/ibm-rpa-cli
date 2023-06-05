using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class EnvironmentCommand
    {
        [RequiresProject]
        internal class NewEnvironmentCommand : Command
        {
            public static readonly string CommandName = "new";
            public NewEnvironmentCommand() : base(CommandName, "Configures environments")
            {
                var alias = new Argument<string>("alias", "The environment name");
                var url = new Option<string?>("--url", $"The server domain url. You can specify '{ServerAddress.DefaultOptionName}' to use {ServerAddress.DefaultUrl}.");
                var region = new Option<string?>("--region", "The region you want to connect.");
                var userName = new Option<string?>("--userName", "The user name, usually the e-mail, to use for authentication.");
                var tenant = new Option<int?>("--tenant", "The tenant code to use for authentication.");
                var password = new Option<string?>("--password", "The user password.");

                AddArgument(alias);
                AddOption(url);
                AddOption(region);
                AddOption(userName);
                AddOption(tenant);
                AddOption(password);

                this.SetHandler(HandleAsync,
                    new RemoteOptionsBinder(alias, url, region, userName, tenant, password),
                    Bind.FromLogger<EnvironmentCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<ISecretProvider>(),
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(RemoteOptions options, ILogger<EnvironmentCommand> logger,
                IRpaClientFactory clientFactory, ISecretProvider secretProvider, IProject project, InvocationContext context) =>
                await HandleAsync(options, (ILogger)logger, clientFactory, secretProvider, project, context);

            public async Task HandleAsync(RemoteOptions options, ILogger logger,
                IRpaClientFactory clientFactory, ISecretProvider secretProvider, IProject project, InvocationContext context)
            {
                var handler = new NewEnvironmentHandler(logger, project, context.Console, clientFactory, secretProvider);
                await handler.HandleAsync(options, context.GetCancellationToken());
            }

            internal class NewEnvironmentHandler
            {
                private readonly ILogger logger;
                private readonly IProject project;
                private readonly IConsole console;
                private readonly IRpaClientFactory clientFactory;
                private readonly ISecretProvider secretProvider;

                internal NewEnvironmentHandler(ILogger logger, IProject project, IConsole console, IRpaClientFactory clientFactory, ISecretProvider secretProvider)
                {
                    this.logger = logger;
                    this.project = project;
                    this.console = console;
                    this.clientFactory = clientFactory;
                    this.secretProvider = secretProvider;
                }

                internal async Task HandleAsync(RemoteOptions options, CancellationToken cancellation)
                {
                    project.EnsureCanConfigure(options.Alias);
                    var password = secretProvider.GetSecret(options);
                    var regionSelector = new RegionSelector(console, clientFactory, project);
                    var region = await regionSelector.SelectAsync(options.Address, options.RegionName, cancellation);

                    using var client = clientFactory.CreateFromRegion(region);
                    var accountSelector = new AccountSelector(console, client.Account);
                    var credentials = await accountSelector.SelectAsync(options.UserName, options.TenantCode, password, cancellation);
                    var environment = await project.ConfigureEnvironment(client.Account, options.Alias, region, credentials, cancellation);
                    await project.SaveAsync(cancellation);

                    logger.LogInformation("Hi '{PersonName}', the following environment has been configured: {Environment}\n" +
                        "Use '{RpaCommandName} {PullCommand} [asset name] --env {Alias}' to pull files from the environment.\n" +
                        "Use '{RpaCommandName} {DeployCommand} {Environment}' to deploy the project to the environment.",
                        environment.Session.Current.PersonName, environment, RpaCommand.CommandName, PullCommand.CommandName, environment.Alias,
                        RpaCommand.CommandName, DeployCommand.CommandName, environment.Alias);
                }
            }
        }
    }
}