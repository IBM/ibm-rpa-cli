namespace Joba.IBM.RPA.Cli
{
    internal class ServerSelector
    {
        private readonly Version supportedServerVersion;
        private readonly IConsole console;
        private readonly IRpaClientFactory clientFactory;
        private readonly IProject project;

        public ServerSelector(Version supportedServerVersion, IConsole console, IRpaClientFactory clientFactory, IProject project)
        {
            this.supportedServerVersion = supportedServerVersion;
            this.console = console;
            this.clientFactory = clientFactory;
            this.project = project;
        }

        public async Task<ServerConfig> SelectAsync(ServerAddress address, CancellationToken cancellation)
        {
            address = address.IsDefined ? address : SelectServerAddress();
            using var client = clientFactory.CreateFromAddress(address.ToUri());

            var server = await client.GetConfigurationAsync(cancellation);
            server.EnsureValid(supportedServerVersion);

            return server;
        }

        private ServerAddress SelectServerAddress()
        {
            var configured = project.GetConfiguredRemoteAddresses();
            if (configured.Any())
            {
                var choices = configured.Select(c => c.ToString()).ToArray();
                var choice = console.ShowMenu("Please provide the server address, or ESC to provide a new one", choices);
                if (!choice.HasValue)
                {
                    console.Write($"Skipped. Type the server address ('{ServerAddress.DefaultOptionName}' to use the default): ");
                    return new ServerAddress(Console.ReadLine());
                }

                return new ServerAddress(choices[choice.Value]);
            }

            console.Write($"Type the server address ('{ServerAddress.DefaultOptionName}' to use the default): "); //TODO: empty instead of 'default'
            return new ServerAddress(Console.ReadLine());
        }
    }

    struct ServerAddress
    {
        internal const string Domain = "wdgautomation.com";
        internal const string DefaultUrl = $"https://api.{Domain}/v1.0/";
        internal const string DefaultOptionName = "default";
        private static readonly IDictionary<string, string> appToApiMappings;
        private readonly Uri? address;

        static ServerAddress()
        {
            appToApiMappings = new Dictionary<string, string>
            {
                { "ap1qaapp", "ap1qaapi" },
                { "us1qaapp", "us1qaapi" },
                { "uk1qaapp", "uk1qaapi" },
                { "br1qaapp", "br1qaapi" },
                { "eu1qaapp", "eu1qaapi" },
                { "br2-app", "api" },
                { "ap1app", "ap1api" },
                { "us1app", "us1api" },
                { "br1app", "br1api" },
                { "eu1app", "eu1api" },
            };
        }

        internal ServerAddress(string? url)
        {
            if (url == DefaultOptionName)
                address = BuildUri(DefaultUrl);
            else if (!string.IsNullOrEmpty(url))
                address = BuildUri(url);
        }

        internal bool IsDefined => address != null;
        internal Uri ToUri() => address ?? throw new InvalidOperationException("The address is empty for this instance.");
        public override string ToString() => address != null ? $"{address}" : "<empty>";

        private static Uri BuildUri(string url)
        {
            var builder = new UriBuilder(url) { Query = string.Empty, Fragment = string.Empty };
            if (string.IsNullOrEmpty(builder.Path) || builder.Path == "/")
                builder.Path = "/v1.0/";

            if (builder.Host.EndsWith(Domain))
            {
                var parts = builder.Host.Split('.');
                if (parts.Length > 1)
                {
                    var subDomain = parts[0];
                    if (appToApiMappings.TryGetValue(subDomain, out var apiDomain))
                        builder.Host = builder.Host.Replace(subDomain, apiDomain);
                }
            }

            return builder.Uri;
        }
    }
}
