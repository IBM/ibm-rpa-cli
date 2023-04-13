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
                    Bind.FromServiceProvider<IPackageManagerFactory>(),
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(ILogger<RestorePackageCommand> logger, IPackageManagerFactory packageManagerFactory, IProject project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var manager = packageManagerFactory.Create(project);
                var packages = await manager.RestoreAsync(cancellation);
                if (packages.Any())
                    logger.LogInformation("Total of {Count} packages have been restored.", packages.Count());
                else
                    logger.LogInformation("No packages to restore.");
            }
        }
    }
}