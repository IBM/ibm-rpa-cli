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
            session = new SessionManager(this.userFile, this.userSettings, remote);
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

    internal class SessionManager : ISessionManager
    {
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
        private readonly RemoteSettings remote;

        public SessionManager(UserSettingsFile userFile, UserSettings userSettings, RemoteSettings remote)
        {
            this.userFile = userFile;
            this.userSettings = userSettings;
            this.remote = remote;
        }

        Session ISessionManager.Current => userSettings.Session ?? Session.NoSession;

        async Task<Session> ISessionManager.RenewAndSaveAsync(IAccountResource resource, string password, CancellationToken cancellation)
        {
            var currentSession = userSettings.Session ?? throw new InvalidOperationException("A previous session needs to be available in order to renew it.");
            if (!currentSession.IsExpired)
                return currentSession;

            return await CreateAndSaveAsync(resource, currentSession.UserName, password, cancellation);
        }

        public async Task<Session> CreateAndSaveAsync(IAccountResource resource, string userName, string password, CancellationToken cancellation)
        {
            var credentials = new AccountCredentials(remote.TenantCode, userName, password);
            var internalSession = await credentials.AuthenticateAsync(resource, cancellation);
            var session = Session.From(internalSession);
            userSettings.Session = session;

            await userFile.SaveAsync(userSettings, cancellation);
            return session;
        }
    }

    public interface ISessionManager
    {
        Session Current { get; }
        Task<Session> RenewAndSaveAsync(IAccountResource resource, string password, CancellationToken cancellation);
        Task<Session> CreateAndSaveAsync(IAccountResource resource, string userName, string password, CancellationToken cancellation);
    }
}