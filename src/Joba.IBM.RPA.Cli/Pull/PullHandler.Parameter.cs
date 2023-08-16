using Joba.IBM.RPA.Server;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal class ParameterPullHandler : IPullHandler
    {
        private readonly ILogger logger;
        private readonly IRpaClientFactory clientFactory;
        private readonly IConsole console;
        private readonly IProject project;

        public ParameterPullHandler(ILogger logger, IRpaClientFactory clientFactory, IConsole console, IProject project)
        {
            this.logger = logger;
            this.clientFactory = clientFactory;
            this.console = console;
            this.project = project;
        }

        async Task IPullHandler.HandleAsync(NamePattern pattern, string environmentName, CancellationToken cancellation)
        {
            var client = clientFactory.CreateFromEnvironment(project.Environments[environmentName]);
            var pullService = new ParameterPullService(client, project, environmentName);

            if (pattern.HasWildcard)
            {
                pullService.Many.ShouldContinueOperation += OnShouldPullingAllFiles;
                pullService.Many.Pulling += OnPulling;
                pullService.Many.Pulled += OnManyPulled;

                logger.LogInformation("Pulling parameters '{pattern}' from '{environmentName}' environment...", pattern, environmentName);
                await pullService.Many.PullAsync(pattern, cancellation);

                //StatusCommand.Handle(project, environment, context);
            }
            else
            {
                pullService.One.ShouldContinueOperation += OnShouldPullingOneFile;
                pullService.One.Pulled += OnOnePulled;
                await pullService.One.PullAsync(pattern.Name, cancellation);
            }

            await project.SaveAsync(cancellation);
        }

        void OnOnePulled(object? sender, PulledOneEventArgs<Parameter> e)
        {
            if (e.Change == PulledOneEventArgs<Parameter>.ChangeType.NoChange)
                logger.LogInformation("From {Environment}\nNo change. '{ResourceName}' already has the server value.", e.Alias, e.Resource.Name);
            else if (e.Change == PulledOneEventArgs<Parameter>.ChangeType.Created)
                logger.LogInformation("From {Environment}\n'{ResourceName}' has been created from the server value '{Value}'.", e.Alias, e.Resource.Name, e.Resource.Value);
            else
                logger.LogInformation("From {Environment}\n'{ResourceName}' has been updated to '{Value}'.", e.Alias, e.Resource.Name, e.Resource.Value);
        }

        void OnManyPulled(object? sender, PulledAllEventArgs e)
        {
            if (e.Total == 0)
                logger.LogInformation("No parameters found that matches '{Pattern}' pattern.", e.Pattern);
        }

        void OnPulling(object? sender, PullingEventArgs e)
        {
            if (e.Current.HasValue && e.Total.HasValue && !string.IsNullOrEmpty(e.ResourceName))
                logger.LogDebug("({Current}/{Total}) pulling '{ResourceName}'", e.Current, e.Total, e.ResourceName);
        }

        void OnShouldPullingAllFiles(object? sender, ContinueOperationEventArgs e)
        {
            e.Continue = true;
            //TODO: add --interactive option, and by default is non-interactive
            //using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
            //e.Continue = console.YesOrNo(
            //    $"This operation will pull the server parameters that matches '{e.Pattern}'. " +
            //    $"This will overwrite the current local parameters' values. This is irreversible. " +
            //    $"Are you sure you want to continue? [y/n]");
        }

        void OnShouldPullingOneFile(object? sender, ContinueOperationEventArgs<Parameter> e)
        {
            e.Continue = true;
            //TODO: add --interactive option, and by default is non-interactive
            //using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
            //e.Continue = console.YesOrNo($"This operation will fetch and overwrite the parameter '{e.Resource.Name}' with the latest server value. This is irreversible. " +
            //    $"Are you sure you want to continue? [y/n]");
        }
    }
}