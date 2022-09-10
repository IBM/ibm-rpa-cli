namespace Joba.IBM.RPA.Cli
{
    [RequiresProject]
    internal class SwitchCommand : Command
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
            var (switched, environment) = await project.SwitchToAsync(environmentName, cancellation);
           
            if (switched)
            {
                var client = RpaClientFactory.CreateFromEnvironment(environment);
                var sessionEnsurer = new EnvironmentSessionEnsurer(client, environment);
                _ = await sessionEnsurer.EnsureAsync(cancellation);
                await project.SaveAsync(cancellation);
                ExtendedConsole.WriteLine($"Switched to {environmentName:blue}");
            }
            else
                ExtendedConsole.WriteLine($"Already on {environmentName:blue}");
        }
    }
}