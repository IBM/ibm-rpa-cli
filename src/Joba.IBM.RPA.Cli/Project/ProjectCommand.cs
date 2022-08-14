namespace Joba.IBM.RPA.Cli
{
    class ProjectCommand : Command
    {
        public ProjectCommand() : base("project", "Creates or initializes a RPA project")
        {
            var name = new Argument<string>("name", "The project name");
            AddArgument(name);

            this.SetHandler(HandleAsync, name, Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string name, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var project = Project.CreateFromCurrentDirectory(name);

            //configure the environment
            var command = new EnvironmentCommand();
            await command.HandleAsync(new EnvironmentCommand.Options(Environment.Development), project, cancellation);
        }

        //internal class FetchCommand : Command
        //{
        //    public FetchCommand() : base("fetch", "Fetches the latest WAL file version")
        //    {
        //        var fileArgument = new Argument<string>("file", () => string.Empty, "The WAL file to fetch");
        //        AddArgument(fileArgument);

        //        this.SetHandler(Handle, fileArgument,
        //            Bind.FromServiceProvider<RpaClient>(),
        //            Bind.FromServiceProvider<InvocationContext>());
        //    }

        //    private async Task Handle(string fileName, RpaClient client, InvocationContext context)
        //    {
        //        var cancellation = context.GetCancellationToken();
        //        var project = Project.Load();

        //        if (string.IsNullOrEmpty(fileName))
        //        {
        //            //TODO: fetch all
        //        }
        //        else
        //        {
        //            var wal = project.Get(fileName);

        //            if (!project.Settings.OverwriteOnFetch)
        //            {
        //                //Console.ForegroundColor = ConsoleColor.Yellow;
        //                //ExtendedConsole.WriteLine($"This operation will fetch and update the file ${wal.Info.Name:blue} with the latest server version. Are you sure you want to continue?");
        //                //Console.ResetColor();

        //                var choice = ExtendedConsole.ShowMenu($"This operation will fetch and update the file '{wal.Info.Name}' with the latest server version. Are you sure you want to continue?",
        //                    "No", "Yes", "Yes, do not ask me again");
        //                if (!choice.HasValue)
        //                    throw new OperationCanceledException("User did not provide an answer");
        //                if (choice == 0)
        //                    throw new OperationCanceledException();
        //                else if (choice == 2) //yes, do not ask again
        //                {
        //                    project.Settings.AlwaysOverwriteOnFetch();
        //                    await project.SaveAsync(cancellation);
        //                }
        //            }

        //            await wal.UpdateToLatestAsync(client.Script, cancellation);
        //        }
        //    }
        //}
    }

    class StatusCommand : Command
    {
        public StatusCommand() : base("status", "Inspects the status of the project and files in the current environment")
        {
            var name = new Argument<string>("name", "The project name");

            this.SetHandler(HandleAsync,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(Project project, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
        }
    }
}