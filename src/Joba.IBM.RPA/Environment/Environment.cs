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

        internal Environment(string alias, DirectoryInfo envDir, RemoteSettings remote, UserSettingsFile userFile, UserSettings? userSettings,
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
            session = new SessionManager(this.userFile, this.userSettings);
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

    public interface ISessionManager
    {
        Session Current { get; }
        Task<Session> RenewAndSaveAsync(IAccountResource resource, string password, CancellationToken cancellation);
    }

    internal class SessionManager : ISessionManager
    {
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;

        public SessionManager(UserSettingsFile userFile, UserSettings userSettings)
        {
            this.userFile = userFile;
            this.userSettings = userSettings;
        }

        Session ISessionManager.Current => userSettings.Session ?? throw new InvalidOperationException("No current session available.");

        async Task<Session> ISessionManager.RenewAndSaveAsync(IAccountResource resource, string password, CancellationToken cancellation)
        {
            var currentSession = userSettings.Session ?? throw new InvalidOperationException("A previous session needs to be available.");
            if (!currentSession.IsExpired)
                return currentSession;

            var credentials = new AccountCredentials(currentSession.TenantCode, currentSession.UserName, password);
            var internalSession = await credentials.AuthenticateAsync(resource, cancellation);
            var session = Session.From(internalSession);
            userSettings.Session = session;

            await userFile.SaveAsync(userSettings, cancellation);
            return session;

        }
    }
}