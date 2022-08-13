using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;

namespace Joba.IBM.RPA.Cli
{
    internal class ProjectCommand : Command
    {
        public ProjectCommand() : base("project", "Manages project actions")
        {
            var name = new Argument<string>("name", "The project name");
            AddArgument(name);

            this.SetHandler(HandleAsync,
                name,
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string name, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var project = Project.CreateFromCurrentDirectory(name);

            //configure the environment
            var command = new EnvironmentCommand();
            await command.HandleAsync(new EnvironmentCommand.Options(Environment.Development), project, cancellation);
        }

        //internal class FetchCommand : Command
        //{
        //    public FetchCommand() : base("fetch", "Fetches the latest WAL file version")
        //    {
        //        var fileArgument = new Argument<string>("file", () => string.Empty, "The WAL file to fetch");
        //        AddArgument(fileArgument);

        //        this.SetHandler(Handle, fileArgument,
        //            Bind.FromServiceProvider<RpaClient>(),
        //            Bind.FromServiceProvider<InvocationContext>());
        //    }

        //    private async Task Handle(string fileName, RpaClient client, InvocationContext context)
        //    {
        //        var cancellation = context.GetCancellationToken();
        //        var project = Project.Load();

        //        if (string.IsNullOrEmpty(fileName))
        //        {
        //            //TODO: fetch all
        //        }
        //        else
        //        {
        //            var wal = project.Get(fileName);

        //            if (!project.Settings.OverwriteOnFetch)
        //            {
        //                //Console.ForegroundColor = ConsoleColor.Yellow;
        //                //ExtendedConsole.WriteLine($"This operation will fetch and update the file ${wal.Info.Name:blue} with the latest server version. Are you sure you want to continue?");
        //                //Console.ResetColor();

        //                var choice = ExtendedConsole.ShowMenu($"This operation will fetch and update the file '{wal.Info.Name}' with the latest server version. Are you sure you want to continue?",
        //                    "No", "Yes", "Yes, do not ask me again");
        //                if (!choice.HasValue)
        //                    throw new OperationCanceledException("User did not provide an answer");
        //                if (choice == 0)
        //                    throw new OperationCanceledException();
        //                else if (choice == 2) //yes, do not ask again
        //                {
        //                    project.Settings.AlwaysOverwriteOnFetch();
        //                    await project.SaveAsync(cancellation);
        //                }
        //            }

        //            await wal.UpdateToLatestAsync(client.Script, cancellation);
        //        }
        //    }
        //}
    }

    internal class EnvironmentCommand : Command
    {
        public EnvironmentCommand() : base("env", "Provides commands to manage environments")
        {
            var name = new Argument<string>("name", "The environment name").FromAmong(Project.SupportedEnvironments);
            var region = new Option<string>("--region", "The region you want to connect");
            var userName = new Option<string>("--userName", "The user name, usually the e-mail, to use for authentication");
            var tenant = new Option<int?>("--tenant", "The tenant code to use for authentication");

            AddArgument(name);
            AddOption(region);
            AddOption(userName);
            AddOption(tenant);

            this.SetHandler(HandleAsync,
                new OptionsBinder(name, region, userName, tenant),
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(Options options, Project project, InvocationContext context) =>
            await HandleAsync(options, project, context.GetCancellationToken());

        public async Task HandleAsync(Options options, Project project, CancellationToken cancellation)
        {
            using var regionSelector = new RegionSelector();
            var region = await regionSelector.SelectAsync(options.RegionName, cancellation);

            using var client = region.CreateClient();
            var accountSelector = new AccountSelector(client.Account);
            var account = await accountSelector.SelectAsync(options.UserName, options.TenantCode, cancellation);
            var session = await account.AuthenticateAsync(client.Account, cancellation);

            project.ConfigureEnvironmentAndSwitch(options.Name, region, account, session);
            await project.SaveAsync(cancellation);

            ExtendedConsole.WriteLine($"Hi {session.PersonName:blue}, the environment {project.CurrentEnvironment.Name:blue} has been configured:");
            ExtendedConsole.WriteLine($"Region {region.Name:blue}, Tenant {account.TenantCode:blue} - {session.TenantName:blue}");
            ExtendedConsole.WriteLine($"Use the {Constants.CliName:blue} {Name:blue} to configure more environments");
        }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public record struct Options(string Name, string? RegionName = null, string? UserName = null, int? TenantCode = null);
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        class OptionsBinder : BinderBase<Options>
        {
            private readonly Argument<string> nameArgument;
            private readonly Option<string> regionOption;
            private readonly Option<string> userNameOption;
            private readonly Option<int?> tenantCodeOption;

            public OptionsBinder(Argument<string> nameArgument, Option<string> regionOption, Option<string> userNameOption, Option<int?> tenantCodeOption)
            {
                this.nameArgument = nameArgument;
                this.regionOption = regionOption;
                this.userNameOption = userNameOption;
                this.tenantCodeOption = tenantCodeOption;
            }

            protected override Options GetBoundValue(BindingContext bindingContext)
            {
                return new Options(
                    bindingContext.ParseResult.GetValueForArgument(nameArgument),
                    bindingContext.ParseResult.GetValueForOption(regionOption),
                    bindingContext.ParseResult.GetValueForOption(userNameOption),
                    bindingContext.ParseResult.GetValueForOption(tenantCodeOption));
            }
        }
    }
}