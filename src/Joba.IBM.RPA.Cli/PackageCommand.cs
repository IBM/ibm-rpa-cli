namespace Joba.IBM.RPA.Cli
{
    class PackageCommand : Command
    {
        public PackageCommand() : base("package", "Manages package dependencies")
        {
            AddCommand(new AddPackageCommand());
        }

        [RequiresEnvironment]
        class AddPackageCommand : Command
        {
            public AddPackageCommand() : base("add", "Adds packages")
            {
                var name = new Argument<string>("name", "The package name. To add several at once, use '*' at the end, e.g 'MyPackage*'");

                this.SetHandler(HandleAsync, name,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<Environment>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, Project project, Environment environment, InvocationContext context)
            {
                //var cancellation = context.GetCancellationToken();
                //var client = RpaClientFactory.CreateClient(environment);
                //var pullService = new PackagePullService(client, project, environment);
                //var shouldPullMany = name.EndsWith("*");

                //if (shouldPullMany)
                //{
                //    await pullService.Many.PullAsync(name.TrimEnd('*'), cancellation);
                //}
                //else
                //{
                //    await pullService.One.PullAsync(name, cancellation);
                //}

                //await project.SaveAsync(cancellation);
                //await environment.SaveAsync(cancellation);
            }
        }
    }
}