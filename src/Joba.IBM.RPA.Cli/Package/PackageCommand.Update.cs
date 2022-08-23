namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresEnvironment]
        internal class UpdatePackageCommand : Command
        {
            public const string CommandName = "update";
            public UpdatePackageCommand() : base(CommandName, "Updates package dependencies")
            {
                var name = new Argument<string?>("name", "The package name.'") { Arity = ArgumentArity.ZeroOrOne };
                var version = new Option<int?>("--version", "The version of the package to update.");

                AddArgument(name);
                AddOption(version);
                this.SetHandler(HandleAsync, name, version,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? name, int? version, Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var client = RpaClientFactory.CreateClient(environment);
                var manager = new PackageManager(client, project, environment);

                if (version.HasValue && name == null)
                    throw new Exception($"You cannot specify the version without specifying the package name.");

                if (name == null)
                {
                    var result = await manager.UpdateAllAsync(cancellation);
                    if (result.HasBeenUpdated)
                    {
                        ExtendedConsole.WriteLine($"Total of {result.Operations.Count()} packages were updated to their latest version:");
                        foreach (var operation in result.Operations.OrderBy(o => o.Old.Name))
                            if (operation.HasBeenUpdated)
                                ExtendedConsole.WriteLineIndented($"{operation.Old.Name,40} {operation.Old.Version} -> {operation.New.Version}", 2);

                        var files = result.Files.ToList();
                        if (files.Any())
                        {
                            ExtendedConsole.WriteLineIndented($"Affected files:");
                            foreach (var file in files.OrderBy(f => f.Name))
                                ExtendedConsole.WriteLineIndented($"{file.Info.Name}", 2);
                        }
                    }
                    else
                        ExtendedConsole.WriteLine($"No action was performed, because either all the packages are already in their latest versions or there are not packages installed.");
                }
                else
                {
                    var result = await manager.UpdateAsync(name, WalVersion.Create(version), cancellation);
                    if (result.HasBeenUpdated)
                    {
                        ExtendedConsole.WriteLine($"Package {result.Old.Name:blue} updated from {result.Old.Version} to {result.New.Version:green}.");
                        if (result.Files.Any())
                        {
                            ExtendedConsole.WriteLineIndented($"Affected files:", 2);
                            foreach (var file in result.Files.OrderBy(f => f.Name))
                                ExtendedConsole.WriteLineIndented($"{file.Info.Name}");
                        }
                    }
                    else
                        ExtendedConsole.WriteLine($"Package {result.Old.Name:blue} is already on the latest version {result.Old.Version}");
                }
            }
        }
    }
}