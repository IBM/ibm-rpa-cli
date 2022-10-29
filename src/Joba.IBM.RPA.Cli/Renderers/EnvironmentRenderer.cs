namespace Joba.IBM.RPA.Cli
{
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