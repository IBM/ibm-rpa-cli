namespace Joba.IBM.RPA
{
    public class Environment
    {
        private readonly DirectoryInfo envDir;
        private readonly LocalWalRepository repository;
        private readonly EnvironmentFile environmentFile;
        private readonly EnvironmentSettings environmentSettings;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
        private readonly DependenciesFile dependenciesFile;
        private EnvironmentDependencies? dependencies;

        internal Environment(bool isDefault, DirectoryInfo envDir, EnvironmentFile environmentFile, RemoteSettings remoteSettings,
            UserSettingsFile userFile, UserSettings userSettings, DependenciesFile dependenciesFile, EnvironmentDependencies dependencies)
            : this(envDir, environmentFile, new EnvironmentSettings { IsDefault = isDefault, Remote = remoteSettings }, userFile, userSettings,
                  dependenciesFile, dependencies)
        { }

        internal Environment(DirectoryInfo envDir, EnvironmentFile environmentFile, EnvironmentSettings environmentSettings,
            UserSettingsFile userFile, UserSettings? userSettings, DependenciesFile dependenciesFile, EnvironmentDependencies? dependencies)
        {
            this.envDir = envDir;
            this.environmentFile = environmentFile;
            this.userFile = userFile;
            this.userSettings = userSettings ?? new UserSettings();
            this.environmentSettings = environmentSettings;
            this.dependenciesFile = dependenciesFile;
            this.dependencies = dependencies;
            repository = new LocalWalRepository(envDir);
        }

        public string Alias => environmentFile.Alias;
        public DirectoryInfo Directory => envDir;
        public RemoteSettings Remote => environmentSettings.Remote;
        public bool IsDefault => environmentSettings.IsDefault;
        public ILocalRepository Files => repository;
        public IEnvironmentDependencies Dependencies => dependencies ??= new EnvironmentDependencies();

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
    }
}