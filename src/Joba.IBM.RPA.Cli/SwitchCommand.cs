namespace Joba.IBM.RPA.Cli
{
    class SwitchCommand : Command
    {
        public SwitchCommand() : base("switch", "Switches between environments")
        {
            var name = new Argument<string>("name", "The environment name");

            AddArgument(name);
            this.SetHandler(HandleAsync, name,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string environmentName, Project project, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var switched = project.SwitchTo(environmentName);
            if (switched)
            {
                await project.SaveAsync(cancellation);
                ExtendedConsole.WriteLine($"Switched to {environmentName:blue}");
            }
            else
                ExtendedConsole.WriteLine($"Already on {environmentName:blue}");
        }
    }
}