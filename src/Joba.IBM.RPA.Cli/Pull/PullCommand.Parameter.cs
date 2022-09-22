using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PullCommand
    {
        [RequiresProject, RequiresEnvironment]
        internal class PullParameterCommand : Command
        {
            public PullParameterCommand() : base("parameter", "Pulls parameters")
            {
                var parameterName = new Argument<string?>("name", "The specific parameter name") { Arity = ArgumentArity.ZeroOrOne };
                AddArgument(parameterName);

                this.SetHandler(HandleAsync, parameterName,
                    Bind.FromLogger<PullParameterCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? parameterName, ILogger<PullParameterCommand> logger, IRpaClientFactory clientFactory, Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var console = context.Console;
                var client = clientFactory.CreateFromEnvironment(environment);
                var pullService = new ParameterPullService(client, project, environment);

                if (!string.IsNullOrEmpty(parameterName))
                {
                    pullService.One.ShouldContinueOperation += OnShouldPullingOneFile;
                    pullService.One.Pulled += OnOnePulled;
                    await pullService.One.PullAsync(parameterName, cancellation);
                }
                else
                {
                    pullService.Many.ShouldContinueOperation += OnShouldPullingAllFiles;
                    pullService.Many.Pulling += OnPulling;
                    pullService.Many.Pulled += OnManyPulled;
                    
                    logger.LogInformation("Pulling parameters from '{ProjectName}' project...", project.Name);
                    await pullService.Many.PullAsync(cancellation);
                    
                    StatusCommand.Handle(project, environment, context);
                }

                await project.SaveAsync(cancellation);
                await environment.SaveAsync(cancellation);

                void OnOnePulled(object? sender, PulledOneEventArgs<Parameter> e)
                {
                    if (e.Change == PulledOneEventArgs<Parameter>.ChangeType.NoChange)
                        logger.LogInformation("From {Environment}\nNo change. '{ResourceName}' already has the server value.", e.Environment, e.Resource.Name);
                    else if (e.Change == PulledOneEventArgs<Parameter>.ChangeType.Created)
                        logger.LogInformation("From {Environment}\n'{ResourceName}' has been created from the server value '{Value}'.", e.Environment, e.Resource.Name, e.Resource.Value);
                    else
                        logger.LogInformation("From {Environment}\n'{ResourceName}' has been updated to '{Value}'.", e.Environment, e.Resource.Name, e.Resource.Value);
                }

                void OnManyPulled(object? sender, PulledAllEventArgs e)
                {
                    if (e.Total == 0)
                        logger.LogInformation("No parameters found for '{ProjectName}' project.", e.Project.Name);
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
                        $"This operation will pull the server parameters configured as dependencies of '{e.Project.Name}' project. " +
                        $"This will overwrite the current local parameters' values, except the ones that do not exist on the server. This is irreversible. " +
                        $"Are you sure you want to continue? [y/n]");
                }

                void OnShouldPullingOneFile(object? sender, ContinueOperationEventArgs<Parameter> e)
                {
                    using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
                    e.Continue = console.YesOrNo($"This operation will fetch and overwrite the parameter '{e.Resource.Name}' with the latest server value. This is irreversible. " +
                        $"Are you sure you want to continue? [y/n]");
                }
            }
        }
    }
}