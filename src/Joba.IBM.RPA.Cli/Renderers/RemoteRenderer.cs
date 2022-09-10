namespace Joba.IBM.RPA.Cli
{
    class RemoteRenderer
    {
        private readonly string alias;
        private readonly int padding;

        public RemoteRenderer(string alias, int padding)
        {
            this.alias = alias;
            this.padding = padding;
        }

        public void Render(RemoteSettings remote)
        {
            var spaces = new string(' ', padding);
            ExtendedConsole.WriteLine($"{spaces}{alias:blue} ({remote.TenantName}), [{remote.Region:blue}]({remote.Address})");
        }
    }
}