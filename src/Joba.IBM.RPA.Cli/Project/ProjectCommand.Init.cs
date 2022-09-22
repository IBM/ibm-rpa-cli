using Microsoft.Extensions.Logging;

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
                    Bind.FromLogger<InitializeProjectCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(ILogger<InitializeProjectCommand> logger, IRpaClientFactory clientFactory, 
                Project project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var environment = await project.GetCurrentEnvironmentAsync(cancellation);
                if (environment == null)
                {
                    logger.LogInformation("The project has been initialized, but no environment has been configured yet. Use '{RpaCommandName} {EnvironmentCommandName} {AddEnvironmentCommandName}' to configure one.",
                        RpaCommand.CommandName, EnvironmentCommand.CommandName, EnvironmentCommand.AddEnvironmentCommand.CommandName);
                }
                else
                {
                    var client = clientFactory.CreateFromEnvironment(environment);
                    var sessionEnsurer = new SessionEnsurer(context.Console, client.Account, environment.Session);
                    var session = await sessionEnsurer.EnsureAsync(cancellation);
                    logger.LogInformation("Hi '{session.PersonName}', the project has been initialized.", session.PersonName);
                }
            }
        }
    }
}