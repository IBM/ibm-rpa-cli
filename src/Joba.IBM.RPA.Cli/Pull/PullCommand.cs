namespace Joba.IBM.RPA.Cli
{
    [RequiresEnvironment]
    partial class PullCommand : Command
    {
        public PullCommand() : base("pull", "Pulls all the project files")
        {
            AddCommand(new PullWalCommand());
            AddCommand(new PullParameterCommand());
            AddCommand(new PullPackageCommand());

            this.SetHandler(HandleAsync,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<Environment>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(Project project, Environment environment, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var client = RpaClientFactory.CreateClient(environment);
            var pullService = new PullService(project, environment, new WalPullService(client, project, environment).Many, new ParameterPullService(client, project, environment).Many);

            pullService.ShouldContinueOperation += OnShouldPullingFiles;
            pullService.Pulling += OnPulling;
            await pullService.PullAsync(cancellation);

            await project.SaveAsync(cancellation);
            await environment.SaveAsync(cancellation);
            StatusCommand.Handle(project, environment);
        }

        private void OnShouldPullingFiles(object? sender, ContinuePullOperationEventArgs e)
        {
            var all = "all";
            e.Continue = ExtendedConsole.YesOrNo(
                $"This operation will pull {all:red} the {e.Project.Name:blue} project files and dependencies from the server. " +
                $"This will overwrite every local file copy and dependencies. This is irreversible. " +
                $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
        }

        private void OnPulling(object? sender, PullingEventArgs e)
        {
            Console.Clear();
            ExtendedConsole.WriteLine($"Pulling from {e.Project.Name:blue} project...");
            if (e.Current.HasValue && e.Total.HasValue && !string.IsNullOrEmpty(e.ResourceName))
                ExtendedConsole.WriteLineIndented($"({e.Current}/{e.Total}) pulling {e.ResourceName:blue}");
        }
    }
}