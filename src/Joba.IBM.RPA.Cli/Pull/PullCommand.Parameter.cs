namespace Joba.IBM.RPA.Cli
{
    partial class PullCommand
    {
        [RequiresEnvironment]
        class PullParameterCommand : Command
        {
            private readonly EnvironmentRenderer environmentRenderer = new();

            public PullParameterCommand() : base("parameter", "Pulls parameters")
            {
                var parameterName = new Argument<string?>("name", "The specific parameter name") { Arity = ArgumentArity.ZeroOrOne };
                AddArgument(parameterName);

                this.SetHandler(HandleAsync, parameterName,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? parameterName, Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var client = RpaClientFactory.CreateClient(environment);
                var pullService = new ParameterPullService(client, project, environment);

                if (!string.IsNullOrEmpty(parameterName))
                {
                    pullService.One.ShouldContinueOperation += OnShouldPullingOneFile;
                    pullService.One.Pulled += OnPulled;
                    await pullService.One.PullAsync(parameterName, cancellation);
                }
                else
                {
                    pullService.All.ShouldContinueOperation += OnShouldPullingAllFiles;
                    pullService.All.Pulling += OnPulling;
                    pullService.All.Pulled += OnPulled;
                    await pullService.All.PullAsync(cancellation);
                    StatusCommand.Handle(project, environment);
                }

                await project.SaveAsync(cancellation);
                await environment.SaveAsync(cancellation);
            }

            private void OnPulled(object? sender, PulledOneEventArgs<Parameter> e)
            {
                ExtendedConsole.Write($"From ");
                environmentRenderer.RenderLine(e.Environment);
                if (e.Change == PulledOneEventArgs<Parameter>.ChangeType.NoChange)
                    ExtendedConsole.WriteLineIndented($"No change. {e.Resource.Name:blue} already has the server value.");
                else if (e.Change == PulledOneEventArgs<Parameter>.ChangeType.Created)
                    ExtendedConsole.WriteLineIndented($"{e.Resource.Name:blue} has been created from the server value {e.Resource.Value:green}.");
                else
                {
                    ExtendedConsole.WriteLineIndented($"{e.Resource.Name:blue} has been updated to {e.Resource.Value:green}.");
                }
            }

            private void OnPulled(object? sender, PulledAllEventArgs e)
            {
                if (e.Total == 0)
                    ExtendedConsole.WriteLine($"No parameters found for {e.Project.Name:blue} project.");
            }

            private void OnPulling(object? sender, PullingEventArgs e)
            {
                Console.Clear();
                ExtendedConsole.WriteLine($"Pulling parameters from {e.Project.Name:blue} project...");
                if (e.Current.HasValue && e.Total.HasValue && !string.IsNullOrEmpty(e.ResourceName))
                    ExtendedConsole.WriteLineIndented($"({e.Current}/{e.Total}) pulling {e.ResourceName:blue}");
            }

            private void OnShouldPullingAllFiles(object? sender, ContinuePullOperationEventArgs e)
            {
                e.Continue = ExtendedConsole.YesOrNo(
                    $"This operation will pull the server parameters which names start with {e.Project.Name:blue}. " +
                    $"This will overwrite the current local parameters' values, except the ones that do not exist on the server. This is irreversible. " +
                    $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
            }

            private void OnShouldPullingOneFile(object? sender, ContinuePullOperationEventArgs<Parameter> e)
            {
                e.Continue = ExtendedConsole.YesOrNo($"This operation will fetch and overwrite the parameter {e.Resource.Name:blue} with the latest server value. This is irreversible. " +
                    $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
            }
        }
    }
}