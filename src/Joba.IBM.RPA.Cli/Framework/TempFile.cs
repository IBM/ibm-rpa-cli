namespace Joba.IBM.RPA.Cli
{
    internal class TempFile : IDisposable
    {
        private readonly FileInfo file;

        private TempFile(FileInfo file)
        {
            this.file = file;
        }

        internal FileInfo Info => file;

        internal static async Task<TempFile> CreateAsync(WalFile wal, string prefix, CancellationToken cancellation)
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            if (!tempDir.Exists)
                tempDir.Create();

            var file = new FileInfo(Path.Combine(tempDir.FullName, $"[{prefix}] {wal.Info.Name}"));
            await File.WriteAllTextAsync(file.FullName, wal.ToString(), cancellation);
            return new TempFile(file);
        }

        internal async Task<string> ReadAsync(CancellationToken cancellation) => await File.ReadAllTextAsync(file.FullName, cancellation);
        public static implicit operator FileInfo(TempFile temp) => temp.Info;

        void IDisposable.Dispose()
        {
            if (file.Exists)
                file.Delete();
        }
    }
}
