using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal class PushCommand : Command
    {
        public PushCommand() : base("push", "Pushes all the project files")
        {
            AddCommand(new PushWalCommand());
        }

        [RequiresProject]
        class PushWalCommand : Command
        {
            public PushWalCommand() : base("wal", "Pushes wal files")
            {
                var fileName = new Argument<string>("fileName", "The specific wal file name");
                var environmentName = new Option<string>("--env", "The alias of the environment to push files to.") { Arity = ArgumentArity.ExactlyOne };

                AddArgument(fileName);
                AddOption(environmentName);
                this.SetHandler(HandleAsync, fileName, environmentName,
                    Bind.FromLogger<PushWalCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string fileName, string environmentName, ILogger<PushWalCommand> logger, IRpaClientFactory clientFactory,
                IProject project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var console = context.Console;
                var client = clientFactory.CreateFromEnvironment(project.Environments[environmentName]);
                var pushService = new WalPushService(client, project, environmentName);

                if (!string.IsNullOrEmpty(fileName))
                {
                    pushService.One.ShouldContinueOperation += OnShouldPushingOneFile;
                    pushService.One.Pushed += OnPushed;
                    await pushService.One.PushAsync(fileName, cancellation);
                }
                else
                {
                    //pushService.Many.ShouldContinueOperation += OnShouldPullingAllFiles;
                    //pushService.Many.Pulling += OnPulling;
                    //pushService.Many.Pulled += OnPulled;
                    //await pushService.Many.PullAsync(cancellation);
                    //StatusCommand.Handle(project, environment);
                }

                void OnPushed(object? sender, PushedOneEventArgs<WalFile> e) => logger.LogInformation("File '{Resource}' published", e.Resource);

                void OnShouldPushingOneFile(object? sender, ContinueOperationEventArgs<WalFile> e)
                {
                    using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
                    if (e.Resource.Version.HasValue)
                        e.Continue = console.YesOrNo($"This operation will push the file '{e.Resource.Info.Name}' to the server, increasing the version from '{e.Resource.Version}' to '{e.Resource.Version.Value.ToInt32() + 1}' " +
                            $"This is irreversible. Are you sure you want to continue? [y/n]");
                    else
                        e.Continue = console.YesOrNo($"This operation will push the file '{e.Resource.Info.Name}' to the server, creating a new version " +
                            $"This is irreversible. Are you sure you want to continue? [y/n]");
                }
            }
        }
    }
}
