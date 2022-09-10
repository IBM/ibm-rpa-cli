namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresProject, RequiresEnvironment]
        internal class RestorePackageCommand : Command
        {
            public RestorePackageCommand() : base("restore", "Restores package dependencies")
            {
                this.SetHandler(HandleAsync,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(Project project, Environment environment, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var factory = new PackageManagerFactory(new RpaClientFactory());
                var manager = factory.Create(project, environment);
                var packages = await manager.RestoreAsync(cancellation);
                if (packages.Any())
                    ExtendedConsole.WriteLine($"Total of {packages.Count()} packages have been restored.");
                else
                    ExtendedConsole.WriteLine($"No packages to restore.");
            }
        }
    }
}