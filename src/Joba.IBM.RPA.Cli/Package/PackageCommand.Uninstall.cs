using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject]
        internal class UninstallPackageCommand : Command
        {
            public const string CommandName = "uninstall";
            public UninstallPackageCommand() : base(CommandName, "Uninstall packages")
            {
                var name = new Argument<string?>("name", "The package name. To uninstall several at once, use '*' at the end, e.g 'MyPackage*'.") { Arity = ArgumentArity.ZeroOrOne };

                AddArgument(name);
                this.SetHandler(HandleAsync, name,
                    Bind.FromLogger<UninstallPackageCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? name, ILogger<UninstallPackageCommand> logger, IRpaClientFactory clientFactory,
                Project project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var factory = new PackageManagerFactory(clientFactory);
                var manager = factory.Create(project);

                IEnumerable<PackageMetadata> packages;
                if (name == null)
                    packages = await manager.UninstallAllAsync(cancellation);
                else
                    packages = await manager.UninstallAsync(new NamePattern(name), cancellation);

                if (packages.Any())
                    logger.LogInformation("Total of {Count} packages has been uninstalled.", packages.Count());
                else
                    logger.LogInformation("No packages to uninstall.");
            }
        }
    }
}