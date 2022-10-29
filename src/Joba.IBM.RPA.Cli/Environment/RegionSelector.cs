namespace Joba.IBM.RPA.Cli
{
    class RegionSelector
    {
        private readonly IConsole console;
        private readonly IRpaClientFactory clientFactory;
        private readonly Project project;

        public RegionSelector(IConsole console, IRpaClientFactory clientFactory, Project project)
        {
            this.console = console;
            this.clientFactory = clientFactory;
            this.project = project;
        }

        public async Task<Region> SelectAsync(ServerAddress address, string? regionName, CancellationToken cancellation)
        {
            address = address.IsDefined ? address : SelectServerAddress();
            using var client = clientFactory.CreateFromAddress(address.ToUri());

            var server = await client.GetConfigurationAsync(cancellation);
            if (!string.IsNullOrEmpty(regionName))
                return server.GetByName(regionName) ??
                    PromptToSelectRegion($"The specified region '{regionName}' does not exist. Please select one:", server.Regions);

            if (server.Regions.Length == 1)
                return server.Regions[0];

            return PromptToSelectRegion("Please, select the region", server.Regions);
        }

        private ServerAddress SelectServerAddress()
        {
            var configured = project.GetConfiguredRemoteAddresses();
            if (configured.Count() > 1)
            {
                var choices = configured.Select(c => c.ToString()).ToArray();
                var choice = console.ShowMenu("Please provide the server address", choices);
                if (!choice.HasValue)
                {
                    console.Write($"Skipped. Type the server address ('{ServerAddress.DefaultOptionName}' to use the default): ");
                    return new ServerAddress(Console.ReadLine());
                }

                return new ServerAddress(choices[choice.Value]);
            }

            console.Write($"Type the server address ('{ServerAddress.DefaultOptionName}' to use the default): ");
            return new ServerAddress(Console.ReadLine());
        }

        private Region PromptToSelectRegion(string title, Region[] regions)
        {
            var choice = console.ShowMenu(title, regions.Select(r => $"[{r.Name}] {r.Description}").ToArray());
            if (!choice.HasValue)
                throw new Exception("User skipped region selection");

            return regions[choice.Value];
        }
    }

    record struct RemoteOptions(string Alias, ServerAddress Address, string? RegionName = null, string? UserName = null, int? TenantCode = null, string? Password = null);

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
