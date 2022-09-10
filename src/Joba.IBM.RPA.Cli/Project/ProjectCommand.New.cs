namespace Joba.IBM.RPA.Cli
{
    internal partial class ProjectCommand
    {
        internal class NewProjectCommand : Command
        {
            public NewProjectCommand() : base("new", "Creates a RPA project in the current directory")
            {
                var name = new Argument<string>("name", "The project name. This will be used as the pattern to fetch wal files if '--pattern' is not specified.");
                var pattern = new Option<string?>("--pattern", "Specifies the pattern to fetch files, e.g 'Assistant*'");
                var environmentName = new Option<string?>("--env", "Specifies the first environment to set up after the project is created.");

                AddArgument(name);
                AddOption(pattern);
                AddOption(environmentName);
                this.SetHandler(HandleAsync, name, pattern, environmentName, Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, string? pattern, string? environmentName, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var project = ProjectFactory.CreateFromCurrentDirectory(name, new NamePattern(pattern ?? name + "*"));
                await project.SaveAsync(cancellation);

                if (string.IsNullOrEmpty(environmentName))
                    ExtendedConsole.WriteLine($"Project {project.Name:blue} has been initialized. " +
                        $"Use {RpaCommand.CommandName:blue} {EnvironmentCommand.CommandName:blue} {EnvironmentCommand.AddEnvironmentCommand.CommandName:blue} to add environments.");
                else
                {
                    var command = new EnvironmentCommand.AddEnvironmentCommand();
                    await command.HandleAsync(new RemoteOptions(environmentName), project, cancellation);
                }
            }
        }
    }
}