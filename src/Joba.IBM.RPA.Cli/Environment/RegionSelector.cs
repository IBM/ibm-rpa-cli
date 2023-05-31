namespace Joba.IBM.RPA.Cli
{
    class RegionSelector
    {
        private readonly IConsole console;

        public RegionSelector(IConsole console) => this.console = console;

        public Region Select(ServerConfig server, string? regionName)
        {
            if (!string.IsNullOrEmpty(regionName))
                return server.GetByName(regionName) ??
                    PromptToSelectRegion($"The specified region '{regionName}' does not exist. Please select one:", server.Regions);

            if (server.Regions.Length == 1)
                return server.Regions[0];

            return PromptToSelectRegion("Please, select the region", server.Regions);
        }

        private Region PromptToSelectRegion(string title, Region[] regions)
        {
            var choice = console.ShowMenu(title, regions.Select(r => $"[{r.Name}] {r.Description}").ToArray());
            if (!choice.HasValue)
                throw new Exception("User skipped region selection");

            return regions[choice.Value];
        }
    }
}
