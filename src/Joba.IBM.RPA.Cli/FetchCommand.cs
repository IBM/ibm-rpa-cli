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
            var client = project.CreateClient();

            if (string.IsNullOrEmpty(fileName))
            {
                var choice = ExtendedConsole.YesOrNo($"This operation will fetch the latest server versions of wal files which names start with {project.Name:blue}. " +
                    $"If there are local copies in the {project.CurrentEnvironment.Name:blue} folder, they will be overwritten. This is irreversible. " +
                    $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);

                if (!choice.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (choice.Value == false)
                    throw new OperationCanceledException("User cancelled the operation");

                //TODO: fetch all
            }
            else
            {
                var wal = project.GetFile(fileName);
                if (wal == null)
                {
                    throw new NotImplementedException();
                    //TODO: fetch latest and create wal file
                }
                else if (!wal.IsFromServer)
                {
                    var choice = ExtendedConsole.YesOrNo($"The wal file {wal.Info.Name:blue} is not downloaded from the server. " +
                        $"This operation will fetch and overwrite the file content with the latest server version. This is irreversible. " +
                        $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);

                    if (!choice.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (choice.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var previousVersion = "local";
                    await wal.OverwriteToLatestAsync(client.Script, fileName, cancellation);
                    ExtendedConsole.WriteLine($"{wal.Info.Name:blue} has been updated from {previousVersion:darkgray} to {wal.Version:green} version. " +
                            $"Close the file in Studio and open it again.");
                }
                else
                {
                    var choice = ExtendedConsole.YesOrNo($"This operation will fetch and overwrite the file {wal.Info.Name:blue} with the latest server version. This is irreversible. " +
                        $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
                    if (!choice.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (choice.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var previousVersion = wal.Version;
                    await wal.UpdateToLatestAsync(client.Script, cancellation);
                    if (previousVersion == wal.Version)
                        ExtendedConsole.WriteLine($"No change. {wal.Info.Name:blue} is already in the latest {wal.Version:blue} version");
                    else
                        ExtendedConsole.WriteLine($"{wal.Info.Name:blue} has been updated from {previousVersion:darkgray} to {wal.Version:green} version. " +
                            $"Close the file in Studio and open it again.");
                }
            }
        }
    }
}