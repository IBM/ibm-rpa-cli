namespace Joba.IBM.RPA
{
    internal class PackageReferences : ILocalRepository<PackageMetadata>
    {
        private readonly IDictionary<string, PackageMetadata> mappings;

        internal PackageReferences()
            : this(new Dictionary<string, WalVersion>()) { }

        internal PackageReferences(IDictionary<string, WalVersion> packages)
        {
            mappings = packages.Select(p => new PackageMetadata(p.Key, p.Value)).ToDictionary(k => k.Name, v => v);
        }

        void ILocalRepository<PackageMetadata>.AddOrUpdate(params PackageMetadata[] packages)
        {
            foreach (var package in packages)
            {
                if (mappings.ContainsKey(package.Name))
                    mappings[package.Name] = package;
                else
                    mappings.Add(package.Name, package);
            }
        }

        void ILocalRepository<PackageMetadata>.Update(PackageMetadata package)
        {
            if (mappings.ContainsKey(package.Name))
                mappings[package.Name] = package;
            else
                throw new Exception($"Could not update the package '{package.Name}' because it does not exist.");
        }

        PackageMetadata? ILocalRepository<PackageMetadata>.Get(string name) =>
            mappings.TryGetValue(name, out var value) ? value : null;
        void ILocalRepository<PackageMetadata>.Remove(string name) => mappings.Remove(name);
        void ILocalRepository<PackageMetadata>.Clear() => mappings.Clear();

        public IEnumerator<PackageMetadata> GetEnumerator() => mappings.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public record class PackageMetadata(string Name, WalVersion Version);

    public record class Package(PackageMetadata Metadata, ScriptVersion Script)
    {
        public static Package From(ScriptVersion version) =>
            new(new PackageMetadata(version.Name, version.Version), version);
    }
}
