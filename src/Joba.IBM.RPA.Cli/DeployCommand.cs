using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    [RequiresProject]
    internal class DeployCommand : Command
    {
        public const string CommandName = "deploy";
        public DeployCommand() : base(CommandName, "Deploys the project to an environment.")
        {
            var target = new Argument<string>("target", "The target environment name to deploy the project.");

            AddArgument(target);
            this.SetHandler(HandleAsync, target,
               Bind.FromLogger<DeployCommand>(),
               Bind.FromServiceProvider<IDeployService>(),
               Bind.FromServiceProvider<IProject>(),
               Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string targetAlias, ILogger<DeployCommand> logger, IDeployService deployService,
            IProject project, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var environment = project.Environments[targetAlias];
            await deployService.DeployAsync(project, environment, cancellation);

            logger.LogInformation("Project '{Project}' successfully deployed to {Environment}", project.Name, $"{environment.Alias} ({environment.Remote.TenantName}), [{environment.Remote.Region}]({environment.Remote.Address})");
        }
    }
}
