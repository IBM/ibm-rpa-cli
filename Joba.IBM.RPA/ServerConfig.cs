namespace Joba.IBM.RPA
{
    internal struct ServerConfig
    {
        private readonly IDictionary<string, Region> regions;

        public Region[] Regions
        {
            get => regions.Values.ToArray();
            init => regions = value.ToDictionary(d => d.Name, v => v);
        }

        public ApiClient CreateClient(string region) => new(HttpFactory.Create(GetApiFor(region)));

        private Uri GetApiFor(string region)
        {
            if (!regions.TryGetValue(region, out var candidate))
                throw new Exception($"The region '{region}' does not exist");

            return new Uri(candidate.ApiUrl);
        }
    }
}
