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
}