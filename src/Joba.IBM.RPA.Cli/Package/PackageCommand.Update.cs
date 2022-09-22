using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject, RequiresEnvironment]
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
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? name, int? version, string? sourceAlias,
                ILogger<UpdatePackageCommand> logger, IRpaClientFactory clientFactory,
                Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var factory = new PackageManagerFactory(clientFactory);
                var manager = factory.Create(project, environment, sourceAlias);

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
                            foreach (var operation in result.Operations.OrderBy(o => o.Old.Name))
                                if (operation.HasBeenUpdated)
                                    logger.LogDebug("{Name} {Old} -> {New}", operation.Old.Name, operation.Old.Version, operation.New.Version);

                            var files = result.Files.ToList();
                            if (files.Any())
                                logger.LogDebug("Affected files: {Files}", string.Join(',', files.OrderBy(f => f.Name).Select(f => f.Info.Name)));
                        }
                    }
                    else
                        logger.LogInformation("No action was performed, because either all the packages are already in their latest versions or there are not packages installed.");
                }
                else
                {
                    var result = await manager.UpdateAsync(name, WalVersion.Create(version), cancellation);
                    if (result.HasBeenUpdated)
                    {
                        logger.LogInformation("Package '{Name}' updated from {Old} to {New}.", result.Old.Name, result.Old.Version, result.New.Version);
                        if (logger.IsEnabled(LogLevel.Debug))
                            if (result.Files.Any())
                                logger.LogDebug("Affected files: {Files}", result.Files.OrderBy(f => f.Name).Select(f => f.Info.Name));
                    }
                    else
                        logger.LogInformation("Package '{Name}' is already on the latest version {Version}", result.Old.Name, result.Old.Version);
                }
            }
        }
    }
}