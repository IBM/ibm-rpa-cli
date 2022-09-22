using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    [RequiresProject]
    internal class EnvironmentCommand : Command
    {
        public static readonly string CommandName = "env";
        internal EnvironmentCommand() : base(CommandName, "Manages environments")
        {
            AddCommand(new AddEnvironmentCommand());

            this.SetHandler(HandleAsync,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        public Task HandleAsync(Project project, InvocationContext context)
        {
            //TODO: status of environments
            //TODO: get all environment files
            throw new NotImplementedException();
        }

        [RequiresProject]
        internal class AddEnvironmentCommand : Command
        {
            public static readonly string CommandName = "add";
            public AddEnvironmentCommand() : base(CommandName, "Adds environments")
            {
                var alias = new Argument<string>("alias", "The environment name");
                var url = new Option<string?>("--url", $"The server domain url. You can specify '{ServerAddress.DefaultOptionName}' to use {ServerAddress.DefaultUrl}");
                var region = new Option<string?>("--region", "The region you want to connect");
                var userName = new Option<string?>("--userName", "The user name, usually the e-mail, to use for authentication");
                var tenant = new Option<int?>("--tenant", "The tenant code to use for authentication");

                AddArgument(alias);
                AddOption(url);
                AddOption(region);
                AddOption(userName);
                AddOption(tenant);

                this.SetHandler(HandleAsync,
                    new RemoteOptionsBinder(alias, url, region, userName, tenant),
                    Bind.FromLogger<EnvironmentCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(RemoteOptions options, ILogger<EnvironmentCommand> logger,
                IRpaClientFactory clientFactory, Project project, InvocationContext context) =>
                await HandleAsync(options, (ILogger)logger, clientFactory, project, context);

            public async Task HandleAsync(RemoteOptions options, ILogger logger,
                IRpaClientFactory clientFactory, Project project, InvocationContext context)
            {
                project.EnsureCanConfigure(options.Alias);

                var cancellation = context.GetCancellationToken();
                var regionSelector = new RegionSelector(context.Console, clientFactory, project);
                var region = await regionSelector.SelectAsync(options.Address, options.RegionName, cancellation);

                using var client = clientFactory.CreateFromRegion(region);
                var accountSelector = new AccountSelector(context.Console, client.Account);
                var credentials = await accountSelector.SelectAsync(options.UserName, options.TenantCode, cancellation);
                var environment = await project.ConfigureEnvironmentAndSwitchAsync(client.Account, options.Alias, region, credentials, cancellation);
                await project.SaveAsync(cancellation);
                await environment.SaveAsync(cancellation);

                logger.LogInformation("Hi '{PersonName}', the following environment has been configured: {Environment}\nUse '{RpaCommandName} {EnvironmentCommandName} {Name}' to configure more environments.",
                    environment.Session.Current.PersonName, environment, RpaCommand.CommandName, EnvironmentCommand.CommandName, Name);
            }
        }
    }

    class RemoteOptionsBinder : BinderBase<RemoteOptions>
    {
        private readonly Argument<string> aliasArgument;
        private readonly Option<string?> urlOption;
        private readonly Option<string?> regionOption;
        private readonly Option<string?> userNameOption;
        private readonly Option<int?> tenantCodeOption;

        public RemoteOptionsBinder(Argument<string> aliasArgument, Option<string?> urlOption,
            Option<string?> regionOption, Option<string?> userNameOption, Option<int?> tenantCodeOption)
        {
            this.aliasArgument = aliasArgument;
            this.urlOption = urlOption;
            this.regionOption = regionOption;
            this.userNameOption = userNameOption;
            this.tenantCodeOption = tenantCodeOption;
        }

        protected override RemoteOptions GetBoundValue(BindingContext bindingContext)
        {
            return new RemoteOptions(
                bindingContext.ParseResult.GetValueForArgument(aliasArgument),
                new ServerAddress(bindingContext.ParseResult.GetValueForOption(urlOption)),
                bindingContext.ParseResult.GetValueForOption(regionOption),
                bindingContext.ParseResult.GetValueForOption(userNameOption),
                bindingContext.ParseResult.GetValueForOption(tenantCodeOption));
        }
    }
}