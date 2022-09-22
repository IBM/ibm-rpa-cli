using Microsoft.Extensions.Logging;
using System.CommandLine;

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
           Handle(fileName, project, await project.GetCurrentEnvironmentAsync(context.GetCancellationToken()), context);

        public static void Handle(Project project, Environment environment, InvocationContext context) => Handle(null, project, environment, context);

        private static void Handle(string? fileName, Project project, Environment? environment, InvocationContext context)
        {
            if (environment == null)
                context.Console.WriteLine($"Project {project.Name}. No current environment.");
            else
            {
                var padding = 2;
                var nextPadding = padding + 2;
                var summaryRenderer = new SummaryEnvironmentRenderer(context.Console, padding);
                var walRenderer = new WalFileRenderer(context.Console, project);
                var envRenderer = new EnvironmentRenderer(context.Console, walRenderer, project, nextPadding);

                context.Console.WriteLine($"Project '{project.Name}', on environment");
                summaryRenderer.Render(environment);

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