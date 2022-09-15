namespace Joba.IBM.RPA
{
    class LocalWalRepository : ILocalRepository
    {
        protected readonly DirectoryInfo directory;

        public LocalWalRepository(DirectoryInfo directory) => this.directory = directory;

        DirectoryInfo ILocalRepository.Directory => directory;

        WalFile? ILocalRepository.Get(string name)
        {
            if (!name.EndsWith(WalFile.Extension))
                name = $"{name}{WalFile.Extension}";

            var walFile = new FileInfo(Path.Combine(directory.FullName, name));
            return walFile.Exists ? WalFile.Read(walFile) : null;
        }

        async Task<WalFile> ILocalRepository.DownloadLatestAsync(IScriptResource resource, string name, CancellationToken cancellation)
        {
            var version = await resource.GetLatestVersionAsync(name, cancellation);
            if (version == null)
                throw new Exception($"Could not find the latest version of '{name}'");

            return Create(version);
        }

        async Task<WalFile> ILocalRepository.DownloadAsync(IScriptResource resource, string name, WalVersion version, CancellationToken cancellation)
        {
            var scriptVersion = await resource.GetAsync(name, version, cancellation);
            if (scriptVersion == null)
                throw new Exception($"Could not find the version '{version}' of '{name}'");

            return Create(scriptVersion);
        }

        void ILocalRepository.Delete(string name)
        {
            if (!name.EndsWith(WalFile.Extension))
                name = $"{name}{WalFile.Extension}";

            var walFile = new FileInfo(Path.Combine(directory.FullName, name));
            walFile.Delete();
        }

        public WalFile Create(ScriptVersion version)
        {
            if (!directory.Exists)
                directory.Create();
            var walFile = new FileInfo(Path.Combine(directory.FullName, $"{version.Name}{WalFile.Extension}"));
            return Create(walFile, version);
        }

        protected virtual WalFile Create(FileInfo walFile, ScriptVersion version) => WalFileFactory.Create(walFile, version);

        public IEnumerator<WalFile> GetEnumerator() => directory
            .EnumerateFiles($"*{WalFile.Extension}", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f.Name)
            .Select(WalFile.Read)
            .GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class LocalPackageRepository : LocalWalRepository
    {
        public LocalPackageRepository(DirectoryInfo directory)
            : base(directory) { }

        protected override WalFile Create(FileInfo walFile, ScriptVersion version) => WalFileFactory.CreateAsPackage(walFile, version);
    }

    public interface ILocalRepository : IEnumerable<WalFile>
    {
        DirectoryInfo Directory { get; }
        WalFile? Get(string name);
        WalFile Create(ScriptVersion script);
        Task<WalFile> DownloadLatestAsync(IScriptResource resource, string name, CancellationToken cancellation);
        Task<WalFile> DownloadAsync(IScriptResource resource, string name, WalVersion version, CancellationToken cancellation);
        void Delete(string name);
    }
}