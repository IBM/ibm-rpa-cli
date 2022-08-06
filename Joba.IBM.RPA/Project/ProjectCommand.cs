using System.CommandLine;
using System.CommandLine.Invocation;

namespace Joba.IBM.RPA
{
    internal class ProjectCommand : Command
    {
        public ProjectCommand() : base("project", "Manages project actions")
        {
            AddCommand(new InitCommand());
            AddCommand(new FetchCommand());
        }

        internal class InitCommand : Command
        {
            public InitCommand() : base("init", "Initializes a new project")
            {
                //var nameArgument = new Argument<string>("name", "The project name");
                //AddArgument(nameArgument);

                this.SetHandler(Handle, Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task Handle(InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var project = new Project(Environment.CurrentDirectory);
                await project.CreateAsync(cancellation);

                var files = project.EnumerableWalFiles().ToArray();
                if (files.Length > 0)
                    Console.WriteLine($"Total of {files.Length} files are tracked");
                else
                    Console.Write("Project created, but no wal files were found to be tracked.");
                foreach (var file in files)
                    Console.WriteLine($" {file.Info.Name}");
            }
        }

        internal class FetchCommand : Command
        {
            public FetchCommand() : base("fetch", "Fetches the latest WAL file version")
            {
                var fileArgument = new Argument<string>("file", () => string.Empty, "The WAL file to fetch");
                AddArgument(fileArgument);

                this.SetHandler(Handle, fileArgument,
                    Bind.FromServiceProvider<RpaClient>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task Handle(string fileName, RpaClient client, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var project = Project.Load();

                if (string.IsNullOrEmpty(fileName))
                {
                    //TODO: fetch all
                }
                else
                {
                    var wal = project.Get(fileName);

                    if (!project.Settings.OverwriteOnFetch)
                    {
                        //Console.ForegroundColor = ConsoleColor.Yellow;
                        //ExtendedConsole.WriteLine($"This operation will fetch and update the file ${wal.Info.Name:blue} with the latest server version. Are you sure you want to continue?");
                        //Console.ResetColor();

                        var choice = ExtendedConsole.ShowMenu($"This operation will fetch and update the file '{wal.Info.Name}' with the latest server version. Are you sure you want to continue?",
                            "No", "Yes", "Yes, do not ask me again");
                        if (!choice.HasValue)
                            throw new OperationCanceledException("User did not provide an answer");
                        if (choice == 0)
                            throw new OperationCanceledException();
                        else if (choice == 2) //yes, do not ask again
                        {
                            project.Settings.AlwaysOverwriteOnFetch();
                            await project.SaveAsync(cancellation);
                        }
                    }

                    await wal.UpdateToLatestAsync(client.Script, cancellation);
                }
            }
        }
    }
}