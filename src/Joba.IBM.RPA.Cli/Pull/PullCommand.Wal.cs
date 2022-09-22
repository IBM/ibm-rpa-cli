using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PullCommand
    {
        [RequiresProject, RequiresEnvironment]
        internal class PullWalCommand : Command
        {
            public PullWalCommand() : base("wal", "Pulls wal files")
            {
                var fileName = new Argument<string?>("fileName", "The specific wal file name") { Arity = ArgumentArity.ZeroOrOne };
                AddArgument(fileName);

                this.SetHandler(HandleAsync, fileName,
                    Bind.FromLogger<PullWalCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? fileName, ILogger<PullWalCommand> logger, IRpaClientFactory clientFactory,
                Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var console = context.Console;
                var client = clientFactory.CreateFromEnvironment(environment);
                var pullService = new WalPullService(client, project, environment);

                if (!string.IsNullOrEmpty(fileName))
                {
                    pullService.One.ShouldContinueOperation += OnShouldPullingOneFile;
                    pullService.One.Pulled += OnOnePulled;
                    await pullService.One.PullAsync(fileName, cancellation);
                }
                else
                {
                    pullService.Many.ShouldContinueOperation += OnShouldPullingAllFiles;
                    pullService.Many.Pulling += OnPulling;
                    pullService.Many.Pulled += OnManyPulled;
                    
                    logger.LogInformation("Pulling files from '{ProjectName}' project...", project.Name);
                    await pullService.Many.PullAsync(cancellation);
                    
                    StatusCommand.Handle(project, environment, context);
                }

                await project.SaveAsync(cancellation);
                await environment.SaveAsync(cancellation);

                void OnOnePulled(object? sender, PulledOneEventArgs<WalFile> e)
                {
                    if (e.Change == PulledOneEventArgs<WalFile>.ChangeType.NoChange)
                        logger.LogInformation("From {Environment}\nNo change. '{ResourceName}' is already in the latest '{ResourceVersion}' version.", e.Environment, e.Resource.Info.Name, e.Resource.Version);
                    else if (e.Change == PulledOneEventArgs<WalFile>.ChangeType.Created)
                        logger.LogInformation("From {Environment}\n'{ResourceName}' has been created from the latest server '{ResourceVersion}' version.", e.Environment, e.Resource.Info.Name, e.Resource.Version);
                    else
                    {
                        var previousVersion = e.Previous!.Version.HasValue ? e.Previous.Version.ToString() : "local";
                        logger.LogInformation("From {Environment}\n'{ResourceName}' has been updated from '{PreviousVersion}' to '{ResourceVersion}' version. Close the file in Studio and open it again.", e.Environment, e.Resource.Info.Name, previousVersion, e.Resource.Version);
                    }
                }

                void OnManyPulled(object? sender, PulledAllEventArgs e)
                {
                    if (e.Total == 0)
                        logger.LogInformation("No files found for '{ProjectName}' project.", e.Project.Name);
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
                        $"This operation will pull the latest server file versions of '{e.Project.Name}' project. " +
                        $"If there are local copies in the '{e.Environment.Alias}' ({e.Environment.Directory}) directory, they will be overwritten. This is irreversible. " +
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
    }
}