namespace Joba.IBM.RPA.Cli
{
    class RemoteRenderer
    {
        private readonly IConsole console;
        private readonly string alias;
        private readonly int padding;

        public RemoteRenderer(IConsole console, string alias, int padding)
        {
            this.console = console;
            this.alias = alias;
            this.padding = padding;
        }

        public void Render(RemoteSettings remote)
        {
            var spaces = new string(' ', padding);
            console.WriteLine($"{spaces}{alias:blue} ({remote.TenantName}), [{remote.Region:blue}]({remote.Address})");
        }
    }
}