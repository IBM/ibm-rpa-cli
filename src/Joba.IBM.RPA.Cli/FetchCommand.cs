namespace Joba.IBM.RPA.Cli
{
    class FetchCommand : Command
    {
        public FetchCommand() : base("fetch", "Fetches the project files")
        {
            var fileName = new Argument<string>("fileName", () => string.Empty, "The specific Wal file to fetch");
            AddArgument(fileName);

            this.SetHandler(HandleAsync, fileName,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string fileName, Project project, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();

            if (string.IsNullOrEmpty(fileName))
            {
                //TODO: fetch all
            }
            else
            {
                //var wal = project.Get(fileName);

                //if (!project.Settings.OverwriteOnFetch)
                //{
                //    //Console.ForegroundColor = ConsoleColor.Yellow;
                //    //ExtendedConsole.WriteLine($"This operation will fetch and update the file ${wal.Info.Name:blue} with the latest server version. Are you sure you want to continue?");
                //    //Console.ResetColor();

                //    var choice = ExtendedConsole.ShowMenu($"This operation will fetch and update the file '{wal.Info.Name}' with the latest server version. Are you sure you want to continue?",
                //        "No", "Yes", "Yes, do not ask me again");
                //    if (!choice.HasValue)
                //        throw new OperationCanceledException("User did not provide an answer");
                //    if (choice == 0)
                //        throw new OperationCanceledException();
                //    else if (choice == 2) //yes, do not ask again
                //    {
                //        project.Settings.AlwaysOverwriteOnFetch();
                //        await project.SaveAsync(cancellation);
                //    }
                //}

                //await wal.UpdateToLatestAsync(client.Script, cancellation);
            }
        }
    }
}