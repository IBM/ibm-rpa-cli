namespace Joba.IBM.RPA.Cli
{
    class RegionSelector : IDisposable
    {
        private readonly IRpaClient client;

        public RegionSelector() : this(new RpaClient(HttpRpaFactory.Create(new Uri("https://api.wdgautomation.com/v1.0/"))))
        { }

        public RegionSelector(IRpaClient client) => this.client = client;

        public async Task<Region> SelectAsync(string? regionName, CancellationToken cancellation)
        {
            var server = await client.GetConfigurationAsync(cancellation);

            if (string.IsNullOrEmpty(regionName))
                return PromptToSelectRegion("Please, select the region", server.Regions);

            return server.GetByName(regionName) ??
                PromptToSelectRegion($"The specified region {regionName:red} does not exist. Please select one:", server.Regions);
        }

        private static Region PromptToSelectRegion(string title, Region[] regions)
        {
            var choice = ExtendedConsole.ShowMenu(title, regions.Select(r => $"[{r.Name}] {r.Description}").ToArray());
            if (!choice.HasValue)
                throw new Exception("User skipped region selection");

            return regions[choice.Value];
        }

        private static Region PromptToSelectRegion(ref ConsoleInterpolatedStringHandler builder, Region[] regions)
        {
            var choice = ExtendedConsole.ShowMenu(ref builder, regions.Select(r => $"[{r.Name}] {r.Description}").ToArray());
            if (!choice.HasValue)
                throw new Exception("User skipped region selection");

            return regions[choice.Value];
        }

        public void Dispose() => client?.Dispose();
    }
}
