using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    [RequiresProject, RequiresEnvironment]
    internal partial class PullCommand : Command
    {
        public PullCommand() : base("pull", "Pulls all the project files")
        {
            AddCommand(new PullWalCommand());
            AddCommand(new PullParameterCommand());

            this.SetHandler(HandleAsync,
                Bind.FromLogger<PullCommand>(),
                Bind.FromServiceProvider<IRpaClientFactory>(),
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<Environment>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(ILogger<PullCommand> logger, IRpaClientFactory clientFactory, Project project, Environment environment, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var console = context.Console;
            var client = clientFactory.CreateFromEnvironment(environment);
            var pullService = new PullService(project, environment, new ParameterPullService(client, project, environment).Many, new WalPullService(client, project, environment).Many);
            pullService.ShouldContinueOperation += OnShouldPullingFiles;
            pullService.Pulling += OnPulling;
            
            logger.LogInformation("Pulling from '{ProjectName}' project...", project.Name);
            await pullService.PullAsync(cancellation);

            await project.SaveAsync(cancellation);
            await environment.SaveAsync(cancellation);
            
            StatusCommand.Handle(project, environment, context);

            void OnShouldPullingFiles(object? sender, ContinueOperationEventArgs e)
            {
                using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
                e.Continue = console.YesOrNo(
                    $"This operation will pull all the '{e.Project.Name}' project files and dependencies from the server. " +
                    $"This will overwrite every local file copy and dependencies. This is irreversible. " +
                    $"Are you sure you want to continue? [y/n]");
            }

            void OnPulling(object? sender, PullingEventArgs e)
            {
                if (e.Current.HasValue && e.Total.HasValue && !string.IsNullOrEmpty(e.ResourceName))
                    logger.LogDebug("({Current}/{Total}) pulling {ResourceName}", e.Current, e.Total, e.ResourceName);
            }
        }
    }
}