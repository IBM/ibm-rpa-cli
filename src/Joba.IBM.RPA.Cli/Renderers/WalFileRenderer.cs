namespace Joba.IBM.RPA.Cli
{
    class WalFileRenderer
    {
        private readonly IConsole console;
        private readonly Project project;

        public WalFileRenderer(IConsole console, Project project)
        {
            this.console = console;
            this.project = project;
        }

        public void Render(WalFile wal)
        {
            var isTracked = project.Files.IsTracked(wal.Name);
            var color = isTracked ? Console.ForegroundColor : ConsoleColor.Red;
            var version = wal.Version.HasValue ? wal.Version.Value.ToString("D3") : "---";

            using (console.BeginForegroundColor(color))
                console.WriteLineIndented($"{wal.Info.Name,-40} {version}");
        }
    }
}