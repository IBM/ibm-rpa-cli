namespace Joba.IBM.RPA.Cli
{
    internal class PushCommand : Command
    {
        public PushCommand() : base("push", "Pushes all the project files")
        {
            AddCommand(new PushWalCommand());

            //this.SetHandler(HandleAsync,
            //    Bind.FromServiceProvider<Project>(),
            //    Bind.FromServiceProvider<Environment>(),
            //    Bind.FromServiceProvider<InvocationContext>());
        }

        //private async Task HandleAsync(Project project, Environment environment, InvocationContext context)
        //{
        //    var cancellation = context.GetCancellationToken();
        //}

        [RequiresProject, RequiresEnvironment]
        class PushWalCommand : Command
        {
            public PushWalCommand() : base("wal", "Pushes wal files")
            {
                var fileName = new Argument<string?>("fileName", "The specific wal file name") { Arity = ArgumentArity.ZeroOrOne };
                AddArgument(fileName);

                this.SetHandler(HandleAsync, fileName,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? fileName, Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var client = RpaClientFactory.CreateFromEnvironment(environment);
                var pushService = new WalPushService(client, project, environment);

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

                //await project.SaveAsync(cancellation);
                //await environment.SaveAsync(cancellation);
            }

            private void OnPushed(object? sender, PushedOneEventArgs<WalFile> e)
            {
            }

            private void OnShouldPushingOneFile(object? sender, ContinueOperationEventArgs<WalFile> e)
            {
                if (e.Resource.Version.HasValue)
                    e.Continue = ExtendedConsole.YesOrNo($"This operation will push the file {e.Resource.Info.Name:blue} to the server, increasing the version from {e.Resource.Version} to {e.Resource.Version.Value.ToInt32() + 1} " +
                        $"This is irreversible. Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
                else
                    e.Continue = ExtendedConsole.YesOrNo($"This operation will push the file {e.Resource.Info.Name:blue} to the server, creating a new version " +
                        $"This is irreversible. Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
            }
        }
    }
}
