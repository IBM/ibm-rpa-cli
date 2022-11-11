using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal class WalPullHandler : IPullHandler
    {
        private readonly ILogger logger;
        private readonly IRpaClientFactory clientFactory;
        private readonly IConsole console;
        private readonly IProject project;

        public WalPullHandler(ILogger logger, IRpaClientFactory clientFactory, IConsole console, IProject project)
        {
            this.logger = logger;
            this.clientFactory = clientFactory;
            this.console = console;
            this.project = project;
        }

        async Task IPullHandler.HandleAsync(NamePattern pattern, string environmentName, CancellationToken cancellation)
        {
            var client = clientFactory.CreateFromEnvironment(project.Environments[environmentName]);
            var pullService = new WalPullService(client, project, environmentName);

            if (pattern.HasWildcard)
            {
                pullService.Many.ShouldContinueOperation += OnShouldPullingAllFiles;
                pullService.Many.Pulling += OnPulling;
                pullService.Many.Pulled += OnManyPulled;

                logger.LogInformation("Pulling files '{pattern}' from '{environmentName}' environment...", pattern, environmentName);
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

        void OnOnePulled(object? sender, PulledOneEventArgs<WalFile> e)
        {
            if (e.Change == PulledOneEventArgs<WalFile>.ChangeType.NoChange)
                logger.LogInformation("From {Environment}\nNo change. '{ResourceName}' is already in the latest '{ResourceVersion}' version.", e.Alias, e.Resource.Info.Name, e.Resource.Version);
            else if (e.Change == PulledOneEventArgs<WalFile>.ChangeType.Created)
                logger.LogInformation("From {Environment}\n'{ResourceName}' has been created from the latest server '{ResourceVersion}' version.", e.Alias, e.Resource.Info.Name, e.Resource.Version);
            else
            {
                var previousVersion = e.Previous!.Version.HasValue ? e.Previous.Version.ToString() : "local";
                logger.LogInformation("From {Environment}\n'{ResourceName}' has been updated from '{PreviousVersion}' to '{ResourceVersion}' version. Close the file in Studio and open it again.", e.Alias, e.Resource.Info.Name, previousVersion, e.Resource.Version);
            }
        }

        void OnManyPulled(object? sender, PulledAllEventArgs e)
        {
            if (e.Total == 0)
                logger.LogInformation("No files found that matches '{Pattern}' pattern.", e.Pattern);
        }

        void OnPulling(object? sender, PullingEventArgs e)
        {
            if (e.Current.HasValue && e.Total.HasValue && !string.IsNullOrEmpty(e.ResourceName))
                logger.LogDebug("({Current}/{Total}) pulling '{ResourceName}'", e.Current, e.Total, e.ResourceName);
        }

        void OnShouldPullingAllFiles(object? sender, ContinueOperationEventArgs e)
        {
            using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
            e.Continue = console.YesOrNo(
                $"This operation will pull the latest server file versions that matches '{e.Pattern}'. " +
                $"If there are local copies in the '{e.Alias}' ({e.Project.WorkingDirectory}) directory, they will be overwritten. This is irreversible. " +
                $"Are you sure you want to continue? [y/n]");
        }

        void OnShouldPullingOneFile(object? sender, ContinueOperationEventArgs<WalFile> e)
        {
            using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
            e.Continue = console.YesOrNo($"This operation will fetch and overwrite the file '{e.Resource.Info.Name}' with the latest server version. This is irreversible. " +
                $"Are you sure you want to continue? [y/n]");
        }
    }
}