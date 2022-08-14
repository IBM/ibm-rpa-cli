namespace Joba.IBM.RPA.Cli
{
    class StatusCommand : Command
    {
        public StatusCommand() : base("status", "Inspects the status of the project and files in the current environment")
        {
            //var name = new Argument<string>("name", "The project name");

            this.SetHandler(HandleAsync,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private void HandleAsync(Project project, InvocationContext context)
        {
            //var cancellation = context.GetCancellationToken();

            ExtendedConsole.WriteLine($"Project {project.Name:blue}, on environment {project.CurrentEnvironment.Name:blue}");
        }
    }
}