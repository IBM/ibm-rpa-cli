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
                Bind.FromServiceProvider<IProject>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        public static void Handle(IProject project, InvocationContext context) => Handle(null, project, context);

        private static void Handle(string? fileName, IProject project, InvocationContext context)
        {
            //TODO: ProjectRenderer
            var padding = 2;
            var walRenderer = new WalFileRenderer(context.Console, padding);

            context.Console.WriteLine($"Project '{project.Name}'");

            if (!string.IsNullOrEmpty(fileName))
            {
                var wal = project.Scripts.Get(fileName);
                if (wal == null)
                    throw new Exception($"The file '{fileName}' does not exist");

                walRenderer.Render(wal);
            }
            else
            {
                foreach (var wal in project.Scripts)
                    walRenderer.Render(wal);
            }
        }
    }
}