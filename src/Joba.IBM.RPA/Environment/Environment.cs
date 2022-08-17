using System.Diagnostics;

namespace Joba.IBM.RPA
{
    public class Environment
    {
        private readonly DirectoryInfo envDir;
        private readonly EnvironmentFile environmentFile;
        private readonly EnvironmentSettings environmentSettings;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;

        internal Environment(DirectoryInfo envDir, EnvironmentFile environmentFile, RemoteSettings remoteSettings,
            UserSettingsFile userFile, UserSettings userSettings)
            : this(envDir, environmentFile, new EnvironmentSettings { Remote = remoteSettings }, userFile, userSettings) { }

        internal Environment(DirectoryInfo envDir, EnvironmentFile environmentFile, EnvironmentSettings environmentSettings,
            UserSettingsFile userFile, UserSettings? userSettings = null)
        {
            this.envDir = envDir;
            this.environmentFile = environmentFile;
            this.userFile = userFile;
            this.userSettings = userSettings ?? new UserSettings();
            this.environmentSettings = environmentSettings;
        }

        public string Alias => environmentFile.Alias;
        public DirectoryInfo Directory => envDir;
        public RemoteSettings Remote => environmentSettings.Remote;

        public Session GetSession() => new Session
        {
            AccessToken = userSettings.Token ?? throw new Exception("There is no session token available"),
            PersonName = Remote.PersonName,
            TenantCode = Remote.TenantCode,
            TenantName = Remote.TenantName,
            UserName = Remote.UserName
        };

        public void SetSession(Session session)
        {
            userSettings.Token = session.AccessToken;
        }

        public async Task SaveAsync(CancellationToken cancellation)
        {
            if (!envDir.Exists)
                envDir.Create();

            await environmentFile.SaveAsync(environmentSettings, cancellation);
            await userFile.SaveAsync(userSettings, cancellation);
        }

        public WalFile? GetLocalWal(string fileName)
        {
            if (!fileName.EndsWith(WalFile.Extension))
                fileName = $"{fileName}{WalFile.Extension}";

            var walFile = new FileInfo(Path.Combine(Directory.FullName, fileName));
            return walFile.Exists ? WalFile.Read(walFile) : null;
        }

        public IEnumerable<WalFile> GetLocalWals()
        {
            return Directory
                .EnumerateFiles($"*{WalFile.Extension}", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f.Name)
                .Select(WalFile.Read);
        }

        public async Task<WalFile> CreateWalAsync(IScriptClient client, string fileName, CancellationToken cancellation)
        {
            var version = await client.GetLatestVersionAsync(fileName, cancellation);
            if (version == null)
                throw new Exception($"Could not find the latest version of '{fileName}'");

            return CreateLocalWal(fileName, version);
        }

        private WalFile CreateLocalWal(string fileName, ScriptVersion version)
        {
            if (!fileName.EndsWith(WalFile.Extension))
                fileName = $"{fileName}{WalFile.Extension}";

            var walFile = new FileInfo(Path.Combine(Directory.FullName, fileName));
            return WalFile.Create(walFile, version);
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct EnvironmentFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver()
        };
        public static readonly string Extension = ".env";
        private readonly FileInfo file;

        public EnvironmentFile(DirectoryInfo rpaDirectory, string projectName, string alias)
            : this(new FileInfo(Path.Combine(rpaDirectory.FullName, $"{projectName}.{alias}{Extension}")), projectName, alias) { }

        private EnvironmentFile(FileInfo file, string projectName, string alias)
        {
            this.file = file;
            ProjectName = projectName;
            Alias = alias;
        }

        public string FullPath => file.FullName;
        public bool Exists => file.Exists;
        public string ProjectName { get; }
        public string Alias { get; }

        public async Task SaveAsync(EnvironmentSettings settings, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellation);
        }

        public static async Task<(EnvironmentFile, EnvironmentSettings)> LoadAsync(
            DirectoryInfo rpaDir, string projectName, string alias, CancellationToken cancellation)
        {
            var envFile = new EnvironmentFile(rpaDir, projectName, alias);
            using var stream = File.OpenRead(envFile.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<EnvironmentSettings>(stream, SerializerOptions, cancellation)
                ?? throw new Exception($"Could not load environment '{alias}' from '{envFile}'");

            return (envFile, settings);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}]({Alias}) {ToString()}";
    }

    public class RemoteSettings
    {
        public required string Name { get; init; }
        public required Uri Address { get; init; }
        public required int TenantCode { get; init; }
        public required string TenantName { get; init; }
        public required string PersonName { get; init; }
        public required string UserName { get; init; }

        internal static RemoteSettings Create(Region region, Session session)
        {
            return new RemoteSettings
            {
                Name = region.Name,
                Address = region.ApiUrl,
                PersonName = session.PersonName,
                TenantCode = session.TenantCode,
                TenantName = session.TenantName,
                UserName = session.UserName
            };
        }
    }

    internal class EnvironmentSettings
    {
        public required RemoteSettings Remote { get; init; }
    }
}