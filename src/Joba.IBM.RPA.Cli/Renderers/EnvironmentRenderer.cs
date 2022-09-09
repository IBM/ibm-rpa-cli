namespace Joba.IBM.RPA.Cli
{
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
            var files = environment.Files.ToArray();
            if (files.Any())
            {
                ExtendedConsole.WriteLineIndented($"Wal files:", padding);
                foreach (var wal in files)
                    walRenderer.Render(wal);
            }

            if (environment.Dependencies.Parameters.Any())
            {
                ExtendedConsole.WriteLineIndented($"Parameters:", padding);
                foreach (var parameter in environment.Dependencies.Parameters.OrderBy(p => p.Name))
                {
                    var isTracked = project.Dependencies.Parameters.IsTracked(parameter.Name);
                    var color = isTracked ? Console.ForegroundColor : ConsoleColor.Red;
                    using (ExtendedConsole.BeginForegroundColor(color))
                    {
                        if (isTracked)
                            ExtendedConsole.WriteLineIndented($"{parameter.Name,40}");
                        else
                            ExtendedConsole.WriteLineIndented($"{parameter.Name,40} (local)");
                    }
                }
            }
        }
    }

    class ShallowEnvironmentRenderer
    {
        public void Render(Environment environment) => Render(environment, true, 0);

        public void RenderLine(Environment environment) => Render(environment, true, 0);

        public void RenderLineIndented(Environment environment, int padding) => Render(environment, true, padding);

        private void Render(Environment environment, bool newLine, int padding)
        {
            var spaces = new string(' ', padding);
            ExtendedConsole.Write($"{spaces}{environment.Alias:blue} ({environment.Remote.TenantName}), [{environment.Remote.Region:blue}]({environment.Remote.Address})");
            if (newLine)
                Console.WriteLine();
        }
    }
}