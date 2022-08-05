
namespace Joba.IBM.RPA
{
    internal record struct Region(string Name, Uri ApiUrl, string? Description = null)
    {
        public ApiClient CreateClient() => new(HttpFactory.Create(ApiUrl));
    }

    class RegionSelector : IDisposable
    {
        private readonly ApiClient client;

        public RegionSelector()
        {
            client = new ApiClient(HttpFactory.Create(new Uri("https://api.wdgautomation.com/v1.0/")));
        }

        public async Task<Region> SelectAsync(string? regionName, CancellationToken cancellation)
        {
            var server = await client.GetConfigurationAsync(cancellation);

            if (string.IsNullOrEmpty(regionName))
                return PromptToSelectRegion("Choose the region:", server.Regions);

            return server.GetByName(regionName) ??
                PromptToSelectRegion($"The specified region '{regionName}' does not exist. Please select one:", server.Regions);
        }

        private static Region PromptToSelectRegion(string title, Region[] regions)
        {
            var choice = ExtendedConsole.ShowMenu(title, regions.Select(r => $"[{r.Name}] {r.Description}").ToArray());
            if (!choice.HasValue)
                throw new Exception("User skipped region selection");

            return regions[choice.Value];
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
