namespace Joba.IBM.RPA.Cli
{
    class ProjectCommand : Command
    {
        public ProjectCommand() : base("project", "Creates or initializes a RPA project")
        {
            var name = new Argument<string>("name", "The project name");
            var environmentName = new Option<string>("--env", "Specifies the first environment to set up after the project is created.");
            
            AddArgument(name);
            AddOption(environmentName);
            this.SetHandler(HandleAsync, name, environmentName, Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string name, string? environmentName, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var project = ProjectFactory.CreateFromCurrentDirectory(name);
            await project.SaveAsync(cancellation);

            if (string.IsNullOrEmpty(environmentName))
                ExtendedConsole.WriteLine($"Project {project.Name:blue} has been initialized. " +
                    $"Use {RpaCommand.CommandName:blue} {EnvironmentCommand.CommandName:blue} to add environments.");
            else
            {
                var command = new EnvironmentCommand();
                await command.HandleAsync(new EnvironmentCommand.Options(environmentName), project, cancellation);
            }
        }
    }
}