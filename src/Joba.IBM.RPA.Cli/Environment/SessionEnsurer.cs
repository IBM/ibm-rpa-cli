namespace Joba.IBM.RPA.Cli
{
    class SessionEnsurer
    {
        private readonly IConsole console;
        private readonly IAccountAuthenticatorFactory authenticatorFactory;
        private readonly ISessionManager session;
        private readonly RemoteRenderer remoteRenderer;

        public SessionEnsurer(IConsole console, IAccountAuthenticatorFactory authenticatorFactory, ISessionManager session)
        {
            this.console = console;
            this.authenticatorFactory = authenticatorFactory;
            this.session = session;
            remoteRenderer = new RemoteRenderer(console, session.Alias, 2);
        }

        public async Task<Session> EnsureAsync(CancellationToken cancellation)
        {
            if (session.Current == Session.NoSession)
            {
                console.WriteLine($"You need to provide credentials for:");
                remoteRenderer.Render(session.Remote);
                console.Write("User name: ");
                var userName = console.ReadLine() ?? throw new OperationCanceledException("User did not provide the 'user name'");
                console.Write("Password: ");
                var password = console.Password();

                var authenticator = CreateAuthenticator();
                return await session.CreateAndSaveAsync(authenticator, userName, password, cancellation);
            }

            if (session.Current.IsExpired)
            {
                console.WriteLine($"Your session for '{session.Alias}' is expired. " +
                    $"The {RpaCommand.CommandName} tool will try to log in again with user={session.Current.UserName} to:");
                remoteRenderer.Render(session.Remote);
                console.Write($"Please provide the password: ");
                var password = console.Password();

                var authenticator = CreateAuthenticator();
                return await session.RenewAndSaveAsync(authenticator, password, cancellation);
            }

            return session.Current;
        }

        private IAccountAuthenticator CreateAuthenticator()
        {
            return authenticatorFactory.Create(session.Remote.Deployment, session.Remote.AuthenticationMethod, new Region(session.Remote.Region, session.Remote.Address), session.Remote.Properties);
        }
    }
}