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
        private readonly DependenciesFile dependenciesFile;
        private Dependencies? dependencies;

        internal Environment(bool isDefault, DirectoryInfo envDir, EnvironmentFile environmentFile, RemoteSettings remoteSettings,
            UserSettingsFile userFile, UserSettings userSettings, DependenciesFile dependenciesFile, Dependencies dependencies)
            : this(envDir, environmentFile, new EnvironmentSettings { IsDefault = isDefault, Remote = remoteSettings }, userFile, userSettings,
                  dependenciesFile, dependencies)
        { }

        internal Environment(DirectoryInfo envDir, EnvironmentFile environmentFile, EnvironmentSettings environmentSettings,
            UserSettingsFile userFile, UserSettings? userSettings, DependenciesFile dependenciesFile, Dependencies? dependencies)
        {
            this.envDir = envDir;
            this.environmentFile = environmentFile;
            this.userFile = userFile;
            this.userSettings = userSettings ?? new UserSettings();
            this.environmentSettings = environmentSettings;
            this.dependenciesFile = dependenciesFile;
            this.dependencies = dependencies;
        }

        public string Alias => environmentFile.Alias;
        public DirectoryInfo Directory => envDir;
        public RemoteSettings Remote => environmentSettings.Remote;
        public bool IsDefault => environmentSettings.IsDefault;
        public IEnvironmentDependencies Dependencies => dependencies ??= new Dependencies();

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
            if (dependencies != null)
                await dependenciesFile.SaveAsync(dependencies, cancellation);
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
        public const string Extension = ".json";
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
            var file = new EnvironmentFile(rpaDir, projectName, alias);
            using var stream = File.OpenRead(file.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<EnvironmentSettings>(stream, SerializerOptions, cancellation)
                ?? throw new Exception($"Could not load environment '{alias}' from '{file}'");

            return (file, settings);
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
                Address = region.ApiAddress,
                PersonName = session.PersonName,
                TenantCode = session.TenantCode,
                TenantName = session.TenantName,
                UserName = session.UserName
            };
        }
    }

    internal class EnvironmentSettings
    {
        public required bool IsDefault { get; init; }
        public required RemoteSettings Remote { get; init; }
    }

    internal class UserSettings
    {
        public string? Token { get; set; }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct UserSettingsFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver()
        };
        public const string FileName = "settings.json";
        private readonly FileInfo file;

        public UserSettingsFile(string projectName, string alias)
            : this(new FileInfo(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "rpa", projectName, alias, FileName)))
        { }

        private UserSettingsFile(FileInfo file)
        {
            this.file = file;
        }

        public string FullPath => file.FullName;
        public bool Exists => file.Exists;
        public string ProjectName => file.Directory?.Parent?.Name ?? throw new Exception($"The grandparent directory of '{file.FullName}' should exist");
        public string Alias => file.Directory?.Name ?? throw new Exception($"The parent directory of '{file.FullName}' should exist");

        public async Task SaveAsync(UserSettings userSettings, CancellationToken cancellation)
        {
            if (!file.Directory!.Exists)
                file.Directory.Create();
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, userSettings, SerializerOptions, cancellation);
        }

        public static async Task<(UserSettingsFile, UserSettings?)> LoadAsync(string projectName, string alias, CancellationToken cancellation)
        {
            var file = new UserSettingsFile(projectName, alias);
            if (file.Exists)
            {
                using var stream = File.OpenRead(file.FullPath);
                var settings = await JsonSerializer.DeserializeAsync<UserSettings>(stream, SerializerOptions, cancellation)
                    ?? throw new Exception($"Could not user settings for the project '{projectName}' from '{file}'");

                return (file, settings);
            }

            return (file, null);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}]({Alias}) {ToString()}";
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct DependenciesFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver()
        };
        public const string Extension = ".json";
        private readonly FileInfo file;

        public DependenciesFile(DirectoryInfo environmentDir, string projectName, string alias)
            : this(new FileInfo(Path.Combine(environmentDir.FullName, $"{projectName}.{alias}{Extension}")), projectName, alias) { }

        private DependenciesFile(FileInfo file, string projectName, string alias)
        {
            this.file = file;
            ProjectName = projectName;
            Alias = alias;
        }

        public string FullPath => file.FullName;
        public bool Exists => file.Exists;
        public string ProjectName { get; }
        public string Alias { get; }

        public async Task SaveAsync(Dependencies dependencies, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, dependencies, SerializerOptions, cancellation);
        }

        public static async Task<(DependenciesFile, Dependencies?)> LoadAsync(
            DirectoryInfo environmentDir, string projectName, string alias, CancellationToken cancellation)
        {
            var file = new DependenciesFile(environmentDir, projectName, alias);
            if (!file.Exists)
                return (file, null);

            using var stream = File.OpenRead(file.FullPath);
            var dependencies = await JsonSerializer.DeserializeAsync<Dependencies>(stream, SerializerOptions, cancellation)
                ?? throw new Exception($"Could not load environment '{alias}' project ('{projectName}') dependencies from '{file}'");

            return (file, dependencies);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }

    public interface IEnvironmentDependencies
    {
        void AddOrUpdate(params Parameter[] parameters);
        Parameter? GetParameter(string name);
    }

    internal class Dependencies : IEnvironmentDependencies
    {
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        void IEnvironmentDependencies.AddOrUpdate(Parameter[] parameters)
        {
            foreach (var parameter in parameters)
            {
                if (Parameters.ContainsKey(parameter.Name))
                    Parameters[parameter.Name] = parameter.Value;
                else
                    Parameters.Add(parameter.Name, parameter.Value);
            }
        }

        Parameter? IEnvironmentDependencies.GetParameter(string name) =>
            Parameters.TryGetValue(name, out var value) ? new Parameter(name, value) : null;
    }
}