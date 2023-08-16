using Joba.IBM.RPA.Server;

namespace Joba.IBM.RPA
{
    internal class PackageReferences : IPackages
    {
        private readonly DirectoryInfo packageDirectory;
        private readonly IDictionary<string, PackageMetadata> references;

        internal PackageReferences(DirectoryInfo workingDirectory)
            : this(workingDirectory, new Dictionary<string, WalVersion>()) { }

        internal PackageReferences(DirectoryInfo workingDirectory, IDictionary<string, WalVersion> packages)
        {
            packageDirectory = new DirectoryInfo(Path.Combine(workingDirectory.FullName, PackageSourcesFile.PackagesDirectoryName));
            references = packages.Select(p => new PackageMetadata(p.Key, p.Value)).ToDictionary(k => k.Name, v => v);
        }

        PackageMetadata? IPackages.Get(string name) => references.TryGetValue(name, out var value) ? value : null;

        WalFile IPackages.Install(Package package)
        {
            EnsureDirectory();
            if (references.ContainsKey(package.Metadata.Name))
                references[package.Metadata.Name] = package.Metadata;
            else
                references.Add(package.Metadata.Name, package.Metadata);

            return WalFileFactory.Create(GetFile(package.Metadata), package.Script);
        }

        void IPackages.Uninstall(PackageMetadata metadata)
        {
            var file = GetFile(metadata);
            references.Remove(metadata.Name);
            File.Delete(file.FullName);
        }

        void IPackages.UninstallAll()
        {
            packageDirectory.Delete(true);
            references.Clear();
        }

        WalFile IPackages.Restore(Package package)
        {
            EnsureDirectory();
            return WalFileFactory.Create(GetFile(package.Metadata), package.Script);
        }

        UpdatePackageOperation IPackages.Update(Package package)
        {
            var previous = references[package.Metadata.Name];
            references[package.Metadata.Name] = package.Metadata;
            _ = WalFileFactory.Create(GetFile(package.Metadata), package.Script);
            return new UpdatePackageOperation(previous, package.Metadata);
        }

        WalFile? IPackages.Get(PackageMetadata metadata)
        {
            EnsureDirectory();
            return packageDirectory
                .EnumerateFiles($"{metadata.Name}{WalFile.Extension}", SearchOption.TopDirectoryOnly)
                .Select(WalFile.Read)
                .FirstOrDefault();
        }

        IEnumerable<WalFile> IPackages.EnumerateFiles()
        {
            EnsureDirectory();
            return packageDirectory
                .EnumerateFiles(WalFile.Extension, SearchOption.TopDirectoryOnly)
                .Where(f => references.ContainsKey(Path.GetFileNameWithoutExtension(f.Name)))
                .Select(WalFile.Read);
        }

        private void EnsureDirectory()
        {
            if (!packageDirectory.Exists)
                packageDirectory.Create();
        }
        private FileInfo GetFile(PackageMetadata metadata) => new(Path.Combine(packageDirectory.FullName, $"{metadata.Name}{WalFile.Extension}"));

        public IEnumerator<PackageMetadata> GetEnumerator() => references.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        int ICollection.Count => references.Count;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        void ICollection.CopyTo(Array array, int index) => references.CopyTo((KeyValuePair<string, PackageMetadata>[])array, index);
    }

    public interface IPackages : IEnumerable<PackageMetadata>, ICollection
    {
        PackageMetadata? Get(string name);
        WalFile? Get(PackageMetadata metadata);
        WalFile Install(Package package);
        void Uninstall(PackageMetadata metadata);
        void UninstallAll();
        UpdatePackageOperation Update(Package package);
        WalFile Restore(Package package);
        IEnumerable<WalFile> EnumerateFiles();
    }

    public record class PackageMetadata(string Name, WalVersion Version);
    public record class InstalledPackage(PackageMetadata Metadata, WalFile Wal);
    public record class Package(PackageMetadata Metadata, ScriptVersion Script)
    {
        public static Package From(ScriptVersion version) =>
            new(new PackageMetadata(version.Name, version.Version), version);
    }
}
