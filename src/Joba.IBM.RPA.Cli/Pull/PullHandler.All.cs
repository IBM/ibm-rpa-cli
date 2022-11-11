using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal class PullAllHandler : IPullHandler
    {
        private readonly ILogger logger;
        private readonly IRpaClientFactory clientFactory;
        private readonly IConsole console;
        private readonly IProject project;

        public PullAllHandler(ILogger logger, IRpaClientFactory clientFactory, IConsole console, IProject project)
        {
            this.logger = logger;
            this.clientFactory = clientFactory;
            this.console = console;
            this.project = project;
        }

        async Task IPullHandler.HandleAsync(NamePattern pattern, string environmentName, CancellationToken cancellation)
        {
            var environment = project.Environments[environmentName];
            var client = clientFactory.CreateFromEnvironment(environment);
            var pullService = new PullService(project, environmentName, new ParameterPullService(client, project, environmentName).Many, new WalPullService(client, project, environmentName).Many);
            pullService.ShouldContinueOperation += OnShouldPullingFiles;
            pullService.Pulling += OnPulling;

            logger.LogInformation("Pulling '{pattern}' from '{environmentName}' environment...", pattern, environmentName);
            await pullService.PullAsync(pattern, cancellation);
            await project.SaveAsync(cancellation);

            //StatusCommand.Handle(project, environment, context);
        }

        void OnShouldPullingFiles(object? sender, ContinueOperationEventArgs e)
        {
            using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
            e.Continue = console.YesOrNo(
                $"This operation will pull all the '{e.Pattern}' files and dependencies from the server. " +
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