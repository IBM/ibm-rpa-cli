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

            //after creating the project, automatically configure the 'dev' environment
            var command = new EnvironmentCommand();
            await command.HandleAsync(new EnvironmentCommand.Options(Environment.Development), project, cancellation);
        }
    }
}