namespace Joba.IBM.RPA
{
    class LocalWalRepository : ILocalRepository
    {
        private readonly DirectoryInfo envDirectory;
        private readonly Enumerator enumerator;

        public LocalWalRepository(DirectoryInfo envDirectory)
        {
            this.envDirectory = envDirectory;
            enumerator = new Enumerator(envDirectory);
        }

        WalFile? ILocalRepository.Get(string name)
        {
            if (!name.EndsWith(WalFile.Extension))
                name = $"{name}{WalFile.Extension}";

            var walFile = new FileInfo(Path.Combine(envDirectory.FullName, name));
            return walFile.Exists ? WalFile.Read(walFile) : null;
        }

        async Task<WalFile> ILocalRepository.DownloadLatestAsync(IScriptClient client, string name, CancellationToken cancellation)
        {
            var version = await client.GetLatestVersionAsync(name, cancellation);
            if (version == null)
                throw new Exception($"Could not find the latest version of '{name}'");

            return CreateLocalWal(name, version);
        }

        private WalFile CreateLocalWal(string name, ScriptVersion version)
        {
            if (!name.EndsWith(WalFile.Extension))
                name = $"{name}{WalFile.Extension}";

            var walFile = new FileInfo(Path.Combine(envDirectory.FullName, name));
            return WalFile.Create(walFile, version);
        }

        IEnumerator<WalFile> IEnumerable<WalFile>.GetEnumerator() => enumerator;
        IEnumerator IEnumerable.GetEnumerator() => enumerator;

        struct Enumerator : IEnumerator<WalFile>
        {
            private readonly IEnumerator<WalFile> enumerator;

            public Enumerator(DirectoryInfo dir)
            {
                enumerator = dir
                    .EnumerateFiles($"*{WalFile.Extension}", SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f.Name)
                    .Select(WalFile.Read)
                    .GetEnumerator();
            }

            WalFile IEnumerator<WalFile>.Current => enumerator.Current;
            object IEnumerator.Current => enumerator.Current;
            void IDisposable.Dispose() => enumerator.Dispose();
            bool IEnumerator.MoveNext() => enumerator.MoveNext();
            void IEnumerator.Reset() => enumerator.Reset();
        }
    }

    public interface ILocalRepository : IEnumerable<WalFile>
    {
        WalFile? Get(string name);
        Task<WalFile> DownloadLatestAsync(IScriptClient client, string name, CancellationToken cancellation);
    }
}