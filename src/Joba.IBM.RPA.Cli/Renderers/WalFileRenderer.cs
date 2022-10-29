namespace Joba.IBM.RPA.Cli
{
    class WalFileRenderer
    {
        private readonly IConsole console;
        private readonly int padding;

        public WalFileRenderer(IConsole console, int padding)
        {
            this.console = console;
            this.padding = padding;
        }

        public void Render(WalFile wal)
        {
            var color = wal.IsFromServer ? Console.ForegroundColor : ConsoleColor.Red;
            var version = wal.Version.HasValue ? wal.Version.Value.ToString("D3") : "---";

            using (console.BeginForegroundColor(color))
                console.WriteLineIndented($"{wal.Info.Name,-40} {version}", padding);
        }
    }
}