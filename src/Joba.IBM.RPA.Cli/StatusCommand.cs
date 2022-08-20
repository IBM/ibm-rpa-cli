namespace Joba.IBM.RPA.Cli
{
    class StatusCommand : Command
    {
        public StatusCommand() : base("status", "Inspects the status of the project and files in the current environment")
        {
            var fileName = new Argument<string>("fileName", "The specific wal file name") { Arity = ArgumentArity.ZeroOrOne };

            AddArgument(fileName);
            this.SetHandler(Handle, fileName,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<Environment?>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private void Handle(string fileName, Project project, Environment? environment, InvocationContext context) =>
           Handle(fileName, project, environment);

        public static void Handle(Project project, Environment environment) => Handle(string.Empty, project, environment);

        private static void Handle(string fileName, Project project, Environment? environment)
        {
            if (environment == null)
                ExtendedConsole.WriteLine($"Project {project.Name:blue}. No current environment.");
            else
            {
                var envRenderer = new EnvironmentRenderer();
                var walRenderer = new WalFileRenderer();
                ExtendedConsole.WriteLine($"Project {project.Name:blue}, on environment");
                envRenderer.RenderLineIndented(environment, 2);

                if (!string.IsNullOrEmpty(fileName))
                {
                    var wal = environment.GetLocalWal(fileName);
                    if (wal == null)
                        throw new Exception($"The file '{fileName}' does not exist");

                    walRenderer.Render(wal);
                }
                else
                {
                    ExtendedConsole.WriteLineIndented($"Wal files:", 4);
                    foreach (var wal in environment.GetLocalWals())
                        walRenderer.Render(wal);
                }
            }
        }
    }

    class WalFileRenderer
    {
        public void Render(WalFile wal)
        {
            var color = wal.Version.HasValue ? Console.ForegroundColor : ConsoleColor.Red;
            var version = wal.Version.HasValue ? wal.Version.Value.ToString("D3") : "local";
            using (ExtendedConsole.BeginForegroundColor(color))
            {
                ExtendedConsole.WriteLineIndented($"{wal.Info.Name,40} {version}");
            }
        }
    }
}