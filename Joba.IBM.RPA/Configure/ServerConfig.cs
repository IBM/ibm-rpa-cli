using System.Text.Json;

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

        public Region? GetByName(string name)
        {
            regions.TryGetValue(name, out var region);
            return region;
        }
    }
}
