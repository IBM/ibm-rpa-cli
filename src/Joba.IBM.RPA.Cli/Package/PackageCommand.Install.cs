namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresEnvironment]
        public class InstallPackageCommand : Command
        {
            public const string CommandName = "install";
            public InstallPackageCommand() : base(CommandName, "Installs packages")
            {
                var name = new Argument<string>("name", "The package name. To install several at once, use '*' at the end, e.g 'MyPackage*'.");
                var version = new Option<int?>("--version", "The version of the package to install.");

                AddArgument(name);
                AddOption(version);
                this.SetHandler(HandleAsync, name, version,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, int? version, Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var client = RpaClientFactory.CreateClient(environment);

                var pattern = new NamePattern(name);
                if (pattern.HasWildcard && version.HasValue)
                    throw new Exception($"You cannot specify the version if you're using '*' in the package name.");

                var manager = new PackageManager(client, project, environment);
                if (version.HasValue)
                {
                    var package = await manager.InstallAsync(pattern.Name, new WalVersion(version.Value), cancellation);
                    ExtendedConsole.WriteLine($"Package {package.Name} with version {package.Version:green} has been installed.");
                }
                else
                {
                    var packages = await manager.InstallAsync(pattern, cancellation);
                    ExtendedConsole.WriteLine($"Total of {packages.Count()} packages have been installed.");
                }
            }
        }
    }
}