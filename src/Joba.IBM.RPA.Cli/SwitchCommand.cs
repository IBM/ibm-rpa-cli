namespace Joba.IBM.RPA.Cli
{
    class SwitchCommand : Command
    {
        public SwitchCommand() : base("switch", "Switches between environments")
        {
            var name = new Argument<string>("name", "The environment name").FromAmong(Project.SupportedEnvironments);

            AddArgument(name);
            this.SetHandler(HandleAsync, name,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string environmentName, Project project, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            project.SwitchTo(environmentName);
            await project.SaveAsync(cancellation);

            ExtendedConsole.WriteLine($"Switched to {environmentName:blue}");
        }
    }
}