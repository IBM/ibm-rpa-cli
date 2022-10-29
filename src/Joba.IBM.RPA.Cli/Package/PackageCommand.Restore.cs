using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject]
        internal class RestorePackageCommand : Command
        {
            public RestorePackageCommand() : base("restore", "Restores package dependencies")
            {
                this.SetHandler(HandleAsync,
                    Bind.FromLogger<RestorePackageCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(ILogger<RestorePackageCommand> logger, IRpaClientFactory clientFactory, Project project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var factory = new PackageManagerFactory(clientFactory);
                var manager = factory.Create(project);
                var packages = await manager.RestoreAsync(cancellation);
                if (packages.Any())
                    logger.LogInformation("Total of {Count} packages have been restored.", packages.Count());
                else
                    logger.LogInformation("No packages to restore.");
            }
        }
    }
}