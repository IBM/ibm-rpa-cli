namespace Joba.IBM.RPA.Cli
{
    partial class PackageCommand
    {
        [RequiresEnvironment]
        class RestorePackageCommand : Command
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
                var client = RpaClientFactory.CreateClient(environment);
                var manager = new PackageManager(client, project, environment);
                var packages = await manager.RestoreAsync(cancellation);
                if (packages.Any())
                    ExtendedConsole.WriteLine($"Total of {packages.Count()} packages have been restored.");
                else
                    ExtendedConsole.WriteLine($"No packages to restore.");
            }
        }
    }
}