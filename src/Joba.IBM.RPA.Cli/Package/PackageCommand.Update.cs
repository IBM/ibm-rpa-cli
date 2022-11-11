using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject]
        internal class UpdatePackageCommand : Command
        {
            public const string CommandName = "update";
            public UpdatePackageCommand() : base(CommandName, "Updates package dependencies")
            {
                var name = new Argument<string?>("name", "The package name.'") { Arity = ArgumentArity.ZeroOrOne };
                var version = new Option<int?>("--version", "The version of the package to update.");
                var source = new Option<string?>("--source", "The package source name where the package should be fetched from.");

                AddArgument(name);
                AddOption(version);
                AddOption(source);
                this.SetHandler(HandleAsync, name, version, source,
                    Bind.FromLogger<UpdatePackageCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? name, int? version, string? sourceAlias,
                ILogger<UpdatePackageCommand> logger, IRpaClientFactory clientFactory,
                IProject project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var factory = new PackageManagerFactory(clientFactory);
                var manager = factory.Create(project, sourceAlias);

                if (version.HasValue && name == null)
                    throw new InvalidOperationException($"You cannot specify the version without specifying the package name.");

                if (name == null)
                {
                    var result = await manager.UpdateAllAsync(cancellation);
                    if (result.HasBeenUpdated)
                    {
                        logger.LogInformation("Total of {Count} packages were updated to their latest version.", result.Operations.Count());
                        if (logger.IsEnabled(LogLevel.Debug))
                        {
                            foreach (var operation in result.Operations.OrderBy(o => o.Previous.Name))
                                if (operation.HasBeenUpdated)
                                    logger.LogDebug("{Name} {Previous} -> {New}", operation.Previous.Name, operation.Previous.Version, operation.New.Version);
                        }
                    }
                    else
                        logger.LogInformation("No action was performed, because either all the packages are already in their latest versions or there are not packages installed.");
                }
                else
                {
                    var result = await manager.UpdateAsync(name, WalVersion.Create(version), cancellation);
                    if (result.HasBeenUpdated)
                        logger.LogInformation("Package '{Name}' updated from {Previous} to {New}.", result.Previous.Name, result.Previous.Version, result.New.Version);
                    else
                        logger.LogInformation("Package '{Name}' is already on the latest version {Version}", result.Previous.Name, result.Previous.Version);
                }
            }
        }
    }
}