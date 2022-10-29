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
            this.SetHandler(Handle, fileName,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        public static void Handle(Project project, InvocationContext context) => Handle(null, project, context);

        private static void Handle(string? fileName, Project project, InvocationContext context)
        {
            //TODO: ProjectRenderer
            var padding = 2;
            var walRenderer = new WalFileRenderer(context.Console, padding);

            context.Console.WriteLine($"Project '{project.Name}'");

            if (!string.IsNullOrEmpty(fileName))
            {
                var wal = project.Files.Get(fileName);
                if (wal == null)
                    throw new Exception($"The file '{fileName}' does not exist");

                walRenderer.Render(wal);
            }
            else
            {
                foreach (var wal in project.Files)
                    walRenderer.Render(wal);
            }
        }
    }
}