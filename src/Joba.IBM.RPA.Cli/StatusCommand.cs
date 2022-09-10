namespace Joba.IBM.RPA.Cli
{
    [RequiresProject]
    internal class StatusCommand : Command
    {
        public StatusCommand() : base("status", "Inspects the status of the project and files in the current environment")
        {
            var fileName = new Argument<string?>("fileName", "The specific wal file name") { Arity = ArgumentArity.ZeroOrOne };

            AddArgument(fileName);
            this.SetHandler(HandleAsync, fileName,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string? fileName, Project project, InvocationContext context) =>
           Handle(fileName, project, await project.GetCurrentEnvironmentAsync(context.GetCancellationToken()));

        public static void Handle(Project project, Environment environment) => Handle(null, project, environment);

        private static void Handle(string? fileName, Project project, Environment? environment)
        {
            if (environment == null)
                ExtendedConsole.WriteLine($"Project {project.Name:blue}. No current environment.");
            else
            {
                var padding = 2;
                var nextPadding = padding + 2;
                var shallowRenderer = new ShallowEnvironmentRenderer();
                var walRenderer = new WalFileRenderer(project);
                var envRenderer = new EnvironmentRenderer(walRenderer, project, nextPadding);

                ExtendedConsole.WriteLine($"Project {project.Name:blue}, on environment");
                shallowRenderer.RenderLineIndented(environment, padding);

                if (!string.IsNullOrEmpty(fileName))
                {
                    var wal = environment.Files.Get(fileName);
                    if (wal == null)
                        throw new Exception($"The file '{fileName}' does not exist");

                    walRenderer.Render(wal);
                }
                else
                    envRenderer.Render(environment);
            }
        }
    }
}