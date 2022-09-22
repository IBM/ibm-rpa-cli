namespace Joba.IBM.RPA.Cli
{
    class EnvironmentRenderer
    {
        private readonly IConsole console;
        private readonly WalFileRenderer walRenderer;
        private readonly Project project;
        private readonly int padding;

        public EnvironmentRenderer(IConsole console, WalFileRenderer walRenderer, Project project, int padding)
        {
            this.console = console;
            this.walRenderer = walRenderer;
            this.project = project;
            this.padding = padding;
        }

        public void Render(Environment environment)
        {
            var files = environment.Files.ToArray();
            if (files.Any())
            {
                console.WriteLineIndented($"Wal files:", padding);
                foreach (var wal in files)
                    walRenderer.Render(wal);
            }

            if (environment.Dependencies.Parameters.Any())
            {
                console.WriteLineIndented($"Parameters:", padding);
                foreach (var parameter in environment.Dependencies.Parameters.OrderBy(p => p.Name))
                {
                    var isTracked = project.Dependencies.Parameters.IsTracked(parameter.Name);
                    var color = isTracked ? Console.ForegroundColor : ConsoleColor.Red;
                    using (console.BeginForegroundColor(color))
                    {
                        if (isTracked)
                            console.WriteLineIndented($"{parameter.Name,-40}");
                        else
                            console.WriteLineIndented($"{parameter.Name,-40} (local)");
                    }
                }
            }
        }
    }

    class SummaryEnvironmentRenderer
    {
        private readonly IConsole console;
        private readonly int padding;

        public SummaryEnvironmentRenderer(IConsole console, int padding)
        {
            this.console = console;
            this.padding = padding;
        }

        public void Render(Environment environment)
        {
            var spaces = new string(' ', padding);
            console.WriteLine($"{spaces}{environment.Alias:blue} ({environment.Remote.TenantName}), [{environment.Remote.Region:blue}]({environment.Remote.Address})");
        }
    }
}