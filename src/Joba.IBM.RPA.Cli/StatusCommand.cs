using System;

namespace Joba.IBM.RPA.Cli
{
    class StatusCommand : Command
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
                var walRenderer = new WalFileRenderer();
                var envRenderer = new EnvironmentRenderer(walRenderer, project, nextPadding);

                ExtendedConsole.WriteLine($"Project {project.Name:blue}, on environment");
                shallowRenderer.RenderLineIndented(environment, padding);

                if (!string.IsNullOrEmpty(fileName))
                {
                    var wal = environment.GetLocalWal(fileName);
                    if (wal == null)
                        throw new Exception($"The file '{fileName}' does not exist");

                    walRenderer.Render(wal);
                }
                else
                    envRenderer.Render(environment);
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
                ExtendedConsole.WriteLineIndented($"{wal.Info.Name,40} {version}");
        }
    }

    class EnvironmentRenderer
    {
        private readonly WalFileRenderer walRenderer;
        private readonly Project project;
        private readonly int padding;

        public EnvironmentRenderer(WalFileRenderer walRenderer, Project project, int padding)
        {
            this.walRenderer = walRenderer;
            this.project = project;
            this.padding = padding;
        }

        public void Render(Environment environment)
        {
            //TODO: render from 'project.Files'
            ExtendedConsole.WriteLineIndented($"Wal files:", padding);
            foreach (var wal in environment.GetLocalWals())
                walRenderer.Render(wal);

            if (environment.Dependencies.Parameters.Any())
            {
                ExtendedConsole.WriteLineIndented($"Parameters:", padding);
                foreach (var parameter in environment.Dependencies.Parameters.OrderBy(p => p.Name))
                {
                    var hasParameter = project.Dependencies.Parameters.Contains(parameter.Name);
                    var color = hasParameter ? Console.ForegroundColor : ConsoleColor.Red;
                    using (ExtendedConsole.BeginForegroundColor(color))
                    {
                        if (hasParameter)
                            ExtendedConsole.WriteLineIndented($"{parameter.Name,40}");
                        else
                            ExtendedConsole.WriteLineIndented($"{parameter.Name,40} (local)");
                    }
                }
            }
        }
    }
}