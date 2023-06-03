using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct PackageSourcesFile
    {
        internal const string Extension = ".sources.json";
        internal const string PackagesDirectoryName = "packages";
        private readonly FileInfo file;

        internal PackageSourcesFile(DirectoryInfo workingDir, string projectName)
            : this(new FileInfo(Path.Combine(workingDir.FullName, $"{projectName}{Extension}"))) { }
        private PackageSourcesFile(FileInfo file) => this.file = file;

        internal bool Exists => file.Exists;
        internal string FullPath => file.FullName;
        internal string ProjectName => file.Name.Replace(Extension, null);
        internal DirectoryInfo WorkingDirectory => file.Directory ?? throw new Exception($"The file directory of '{file}' should exist");

        internal async Task SaveAsync(PackageSources sources, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            var serializerOptions = JsonSerializerOptionsFactory.CreateForPackageSources();
            await JsonSerializer.SerializeAsync(stream, sources, serializerOptions, cancellation);
        }

        internal static async Task<(PackageSourcesFile, PackageSources?)> TryLoadAsync(DirectoryInfo workingDir, string projectName,
            ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings, CancellationToken cancellation)
        {
            var file = new PackageSourcesFile(workingDir, projectName);
            if (!file.Exists)
                return (file, null);

            using var stream = File.OpenRead(file.FullPath);
            var serializerOptions = JsonSerializerOptionsFactory.CreateForPackageSources(projectSettings, userFile, userSettings);
            var sources = await JsonSerializer.DeserializeAsync<PackageSources>(stream, serializerOptions, cancellation)
                ?? throw new Exception($"Could not load package sources from '{file}'");

            return (file, sources);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"{ToString()}";
    }

    public record struct PackageSource(string Alias, RemoteSettings Remote, ISessionManager Session)
    {
        public override string ToString() => $"{Alias} ({Remote.TenantName}), [{Remote.Region:blue}]({Remote.Address})";
    }

    internal class PackageSources : IPackageSources
    {
        private readonly ProjectSettings projectSettings;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
        private readonly IDictionary<string, PackageSource> sources;

        public PackageSources(ProjectSettings projectSettings, UserSettingsFile userFile,
            UserSettings userSettings)
            : this(projectSettings, userFile, userSettings, new Dictionary<string, RemoteSettings>()) { }

        public PackageSources(ProjectSettings projectSettings, UserSettingsFile userFile,
        UserSettings userSettings, IDictionary<string, RemoteSettings> sources)
        {
            this.projectSettings = projectSettings;
            this.userFile = userFile;
            this.userSettings = userSettings;
            this.sources = sources.ToDictionary(k => k.Key,
                v => new PackageSource(v.Key, v.Value, new SessionManager(v.Key, userFile, userSettings, v.Value)));
        }

        internal bool SourceExists(string alias) => sources.ContainsKey(alias);

        PackageSource IPackageSources.this[string alias] => sources[alias];
        PackageSource? IPackageSources.Get(string alias) => sources.TryGetValue(alias, out var value) ? value : null;

        async Task<PackageSource> IPackageSources.AddAsync(IAccountResource resource, string alias, Region region, AccountCredentials credentials, CancellationToken cancellation)
        {
            if (sources.ContainsKey(alias))
                throw new ProjectException($"Cannot add package source '{alias}' because it's already added.");
            if (projectSettings.EnvironmentExists(alias))
                throw new ProjectException($"Cannot add package source because the alias '{alias}' needs to be unique among environments and package sources.");

            var session = await credentials.AuthenticateAsync(resource, cancellation);
            var remote = RemoteSettings.Create(region, session);

            var source = new PackageSource(alias, remote, new SessionManager(alias, userFile, userSettings, remote));
            sources.Add(alias, source);
            userSettings.AddOrUpdateSession(alias, Session.From(session));

            return source;
        }

        IEnumerator<PackageSource> IEnumerable<PackageSource>.GetEnumerator() => sources.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => sources.Values.GetEnumerator();
        int ICollection.Count => sources.Count;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        void ICollection.CopyTo(Array array, int index) => sources.CopyTo((KeyValuePair<string, PackageSource>[])array, index);
    }

    public interface IPackageSources : IEnumerable<PackageSource>, ICollection
    {
        PackageSource this[string alias] { get; }
        PackageSource? Get(string alias);
        Task<PackageSource> AddAsync(IAccountResource resource, string alias, Region region, AccountCredentials credentials, CancellationToken cancellation);
    }
}