namespace Joba.IBM.RPA
{
    public class Environment
    {
        private readonly DirectoryInfo envDir;
        private readonly ISessionManager session;
        private readonly LocalWalRepository repository;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
        private readonly EnvironmentDependenciesFile dependenciesFile;
        private EnvironmentDependencies? dependencies;

        internal Environment(string alias, DirectoryInfo envDir, RemoteSettings remote, UserSettingsFile userFile,
            UserSettings userSettings, EnvironmentDependenciesFile dependenciesFile)
            : this(alias, envDir, remote, userFile, userSettings,
                  dependenciesFile, null)
        { }

        internal Environment(string alias, DirectoryInfo envDir, RemoteSettings remote, UserSettingsFile userFile, UserSettings userSettings,
            EnvironmentDependenciesFile dependenciesFile, EnvironmentDependencies? dependencies)
        {
            Alias = alias;
            Remote = remote;
            this.envDir = envDir;
            this.userFile = userFile;
            this.userSettings = userSettings ?? new UserSettings();
            this.dependenciesFile = dependenciesFile;
            this.dependencies = dependencies;
            repository = new LocalWalRepository(envDir);
            session = new SessionManager(alias, this.userFile, this.userSettings, remote);
        }

        public string Alias { get; }
        public RemoteSettings Remote { get; }
        public DirectoryInfo Directory => envDir;
        public ISessionManager Session => session;
        public ILocalRepository Files => repository;
        public IEnvironmentDependencies Dependencies => dependencies ??= new EnvironmentDependencies(envDir);

        public async Task SaveAsync(CancellationToken cancellation)
        {
            if (!envDir.Exists)
                envDir.Create();

            await userFile.SaveAsync(userSettings, cancellation);
            if (dependencies != null)
                await dependenciesFile.SaveAsync(dependencies, cancellation);
        }
    }
}