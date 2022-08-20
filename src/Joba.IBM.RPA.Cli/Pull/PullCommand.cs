using System;
using System.Reflection;

namespace Joba.IBM.RPA.Cli
{
    class PullCommand : Command
    {
        public PullCommand() : base("pull", "Pulls all the project files")
        {
            AddCommand(new PullWalCommand());

            this.SetHandler(HandleAsync,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<Environment>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(Project project, Environment environment, InvocationContext context)
        {
            //TODO: pull everything
            throw new NotImplementedException();
        }

        class PullWalCommand : Command
        {
            private readonly EnvironmentRenderer environmentRenderer = new();

            public PullWalCommand() : base("wal", "Pulls wal files")
            {
                var fileName = new Argument<string>("fileName", "The specific wal file name") { Arity = ArgumentArity.ZeroOrOne };
                AddArgument(fileName);

                this.SetHandler(HandleAsync, fileName,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string fileName, Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var client = RpaClientFactory.CreateClient(environment);
                var pullService = new WalPullService(client, project, environment);

                if (string.IsNullOrEmpty(fileName))
                {
                    pullService.All.ShouldContinueOperation += OnShouldPullingAllFiles;
                    pullService.All.Pulling += OnPulling;
                    pullService.All.Pulled += OnPulled;
                    await pullService.All.PullAsync(cancellation);
                    StatusCommand.Handle(project, environment);
                }
                else
                {
                    pullService.One.ShouldContinueOperation += OnShouldPullingOneFile;
                    pullService.One.Pulled += OnPulled;
                    await pullService.One.PullAsync(fileName, cancellation);
                }
            }

            private void OnPulled(object? sender, PulledOneEventArgs e)
            {
                ExtendedConsole.Write($"From ");
                environmentRenderer.RenderLine(e.Environment);
                if (e.PreviousVersion == e.NewVersion)
                    ExtendedConsole.WriteLineIndented($"No change. {e.File.Info.Name:blue} is already in the latest {e.NewVersion:blue} version.");
                else if (e.NewFile)
                    ExtendedConsole.WriteLineIndented($"{e.File.Info.Name:blue} has been created from the latest server {e.NewVersion:green} version.");
                else
                {
                    var previousVersion = e.PreviousVersion.HasValue ? e.PreviousVersion.ToString() : "local";
                    ExtendedConsole.WriteLineIndented($"{e.File.Info.Name:blue} has been updated from {previousVersion:darkgray} to {e.NewVersion:green} version. " +
                        $"Close the file in Studio and open it again.");
                }
            }

            private void OnPulled(object? sender, PulledAllEventArgs e)
            {
                if (!e.Files.Any())
                    ExtendedConsole.WriteLine($"No files found for {e.Project.Name:blue} project.");
            }

            private void OnPulling(object? sender, PullingEventArgs e)
            {
                Console.Clear();
                ExtendedConsole.WriteLine($"Pulling files from {e.Project.Name:blue} project...");
                ExtendedConsole.WriteLineIndented($"({e.Current}/{e.Total}) fetching {e.Script.Name:blue}");
            }

            private void OnShouldPullingAllFiles(object? sender, ContinueOperationEventArgs e)
            {
                e.Continue = ExtendedConsole.YesOrNo(
                    $"This operation will pull the latest server versions of wal files which names start with {e.Project.Name:blue}. " +
                    $"If there are local copies in the {e.Environment.Alias:blue} ({e.Environment.Directory}) directory, they will be overwritten. This is irreversible. " +
                    $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
            }

            private void OnShouldPullingOneFile(object? sender, ContinueOperationEventArgs e)
            {
                if (e.File != null)
                    e.Continue = ExtendedConsole.YesOrNo($"This operation will fetch and overwrite the file {e.File.Info.Name:blue} with the latest server version. This is irreversible. " +
                        $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
            }
        }

        class PullParameterCommand : Command
        {
            public PullParameterCommand() : base("parameter", "Pulls parameters")
            {
                var parameterName = new Argument<string>("name", "The specific parameter name") { Arity = ArgumentArity.ZeroOrOne };
                AddArgument(parameterName);

                this.SetHandler(HandleAsync, parameterName,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string parameterName, Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var client = RpaClientFactory.CreateClient(environment);

                if (string.IsNullOrEmpty(parameterName))
                {
                    var parameters = (await client.Parameter.SearchAsync(project.Name, 50, cancellation)).Where(s => s.Id.StartsWith(project.Name)).ToArray();
                }
                else
                {

                }
            }
        }
    }
}