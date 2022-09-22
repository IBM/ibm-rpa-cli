using Microsoft.Extensions.Logging;

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
                Bind.FromLogger<SwitchCommand>(),
                Bind.FromServiceProvider<IRpaClientFactory>(),
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string environmentName, ILogger<SwitchCommand> logger, IRpaClientFactory clientFactory,
            Project project, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var (switched, environment) = await project.SwitchToAsync(environmentName, cancellation);

            if (switched)
            {
                var client = clientFactory.CreateFromEnvironment(environment);
                var sessionEnsurer = new SessionEnsurer(context.Console, client.Account, environment.Session);
                _ = await sessionEnsurer.EnsureAsync(cancellation);
                await project.SaveAsync(cancellation);
                logger.LogInformation("Switched to {environmentName}", environmentName);
            }
            else
                logger.LogInformation("Already on {environmentName}", environmentName);
        }
    }
}