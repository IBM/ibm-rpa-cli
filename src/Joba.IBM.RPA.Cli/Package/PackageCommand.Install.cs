using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject, RequiresEnvironment]
        internal class InstallPackageCommand : Command
        {
            public const string CommandName = "install";
            public InstallPackageCommand() : base(CommandName, "Installs packages")
            {
                var name = new Argument<string>("name", "The package name. To install several at once, use '*' at the end, e.g 'MyPackage*'.");
                var version = new Option<int?>("--version", "The version of the package to install.");
                var source = new Option<string?>("--source", "The package source name where the package should be fetched from.");

                AddArgument(name);
                AddOption(version);
                AddOption(source);
                this.SetHandler(HandleAsync, name, version, source,
                    Bind.FromLogger<InstallPackageCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, int? version, string? sourceAlias, ILogger<InstallPackageCommand> logger, IRpaClientFactory clientFactory,
                Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();

                var pattern = new NamePattern(name);
                if (pattern.HasWildcard && version.HasValue)
                    throw new Exception($"You cannot specify the version if you're using '*' in the package name.");

                var factory = new PackageManagerFactory(clientFactory);
                var manager = factory.Create(project, environment, sourceAlias);
                if (version.HasValue)
                {
                    var package = await manager.InstallAsync(pattern.Name, new WalVersion(version.Value), cancellation);
                    logger.LogInformation("Package '{PackageName}' with version {PackageVersion} has been installed.", package.Name, package.Version);
                }
                else
                {
                    var packages = await manager.InstallAsync(pattern, cancellation);
                    logger.LogInformation("Total of {Count} packages have been installed.", packages.Count());
                }
            }
        }
    }
}