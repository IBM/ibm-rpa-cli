﻿using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject]
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
                    Bind.FromServiceProvider<IPackageManagerFactory>(),
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, int? version, string? sourceAlias, ILogger<InstallPackageCommand> logger,
                IPackageManagerFactory packageManagerFactory, IProject project, InvocationContext context)
            {
                var handler = new InstallPackageHandler(logger, project, packageManagerFactory);
                await handler.HandleAsync(name, version, sourceAlias, context.GetCancellationToken());
            }
        }

        internal class InstallPackageHandler
        {
            private readonly ILogger logger;
            private readonly IProject project;
            private readonly IPackageManagerFactory packageManagerFactory;

            public InstallPackageHandler(ILogger logger, IProject project, IPackageManagerFactory packageManagerFactory)
            {
                this.logger = logger;
                this.project = project;
                this.packageManagerFactory = packageManagerFactory;
            }

            internal async Task HandleAsync(string name, int? version, string? sourceAlias, CancellationToken cancellation)
            {
                var pattern = new NamePattern(name);
                if (pattern.HasWildcard && version.HasValue)
                    throw new PackageException(pattern.ToString(), $"You cannot specify the version if you're using '*' in the package name.");

                var manager = packageManagerFactory.Create(project, sourceAlias);
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