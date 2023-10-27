namespace Joba.IBM.RPA
{
    internal class SessionManager : ISessionManager
    {
        private readonly string alias;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
        private readonly RemoteSettings remote;

        public SessionManager(string alias, UserSettingsFile userFile, UserSettings userSettings, RemoteSettings remote)
        {
            this.alias = alias;
            this.userFile = userFile;
            this.userSettings = userSettings;
            this.remote = remote;
        }

        string ISessionManager.Alias => alias;
        RemoteSettings ISessionManager.Remote => remote;
        Session ISessionManager.Current => userSettings.TryGetSession(alias) ?? Session.NoSession;

        async Task<Session> ISessionManager.RenewAndSaveAsync(IAccountAuthenticator authenticator, string password, CancellationToken cancellation)
        {
            var currentSession = userSettings.TryGetSession(alias) ?? throw new InvalidOperationException("A previous session needs to be available in order to renew it.");
            if (!currentSession.IsExpired)
                return currentSession;

            return await CreateAndSaveAsync(authenticator, currentSession.UserName, password, cancellation);
        }

        public async Task<Session> CreateAndSaveAsync(IAccountAuthenticator authenticator, string userName, string password, CancellationToken cancellation)
        {
            var credentials = new AccountCredentials(remote.TenantCode, userName, password);
            var internalSession = await authenticator.AuthenticateAsync(credentials, cancellation);
            var session = Session.From(internalSession);
            userSettings.Sessions[alias] = session;

            await userFile.SaveAsync(userSettings, cancellation);
            return session;
        }
    }

    public interface ISessionManager
    {
        string Alias { get; }
        RemoteSettings Remote { get; }
        Session Current { get; }
        Task<Session> RenewAndSaveAsync(IAccountAuthenticator authenticator, string password, CancellationToken cancellation);
        Task<Session> CreateAndSaveAsync(IAccountAuthenticator authenticator, string userName, string password, CancellationToken cancellation);
    }
}