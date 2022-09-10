namespace Joba.IBM.RPA.Cli
{
    internal partial class ProjectCommand
    {
        [RequiresProject]
        internal class InitializeProjectCommand : Command
        {
            public InitializeProjectCommand() : base("init", "Initializes the already created project in the current directory")
            {
                this.SetHandler(HandleAsync,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(Project project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var environment = await project.GetCurrentEnvironmentAsync(cancellation);
                if (environment == null)
                {
                    ExtendedConsole.WriteLine($"The project has been initialized, but no environment has been configured yet. " +
                        $"Use {RpaCommand.CommandName:blue} {EnvironmentCommand.CommandName:blue} {EnvironmentCommand.AddEnvironmentCommand.CommandName:blue} to configure one.");
                }
                else
                {
                    var client = RpaClientFactory.CreateFromEnvironment(environment);
                    var sessionEnsurer = new SessionEnsurer(client.Account, environment.Session);
                    var session = await sessionEnsurer.EnsureAsync(cancellation);
                    ExtendedConsole.WriteLine($"Hi {session.PersonName:blue}, the project has been initialized.");
                }
            }
        }
    }
}