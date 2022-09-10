namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject, RequiresEnvironment]
        internal class UninstallPackageCommand : Command
        {
            public const string CommandName = "uninstall";
            public UninstallPackageCommand() : base(CommandName, "Uninstall packages")
            {
                var name = new Argument<string?>("name", "The package name. To uninstall several at once, use '*' at the end, e.g 'MyPackage*'.") { Arity = ArgumentArity.ZeroOrOne };

                AddArgument(name);
                this.SetHandler(HandleAsync, name,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string? name, Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var client = RpaClientFactory.CreateFromEnvironment(environment);
                var manager = new PackageManager(client, project, environment);

                IEnumerable<PackageMetadata> packages;
                if (name == null)
                    packages = await manager.UninstallAllAsync(cancellation);
                else
                    packages = await manager.UninstallAsync(new NamePattern(name), cancellation);

                if (packages.Any())
                    ExtendedConsole.WriteLine($"Total of {packages.Count()} packages has been uninstalled.");
                else
                    ExtendedConsole.WriteLine($"No packages to uninstall.");
            }
        }
    }
}