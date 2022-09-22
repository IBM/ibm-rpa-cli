namespace Joba.IBM.RPA.Cli
{
    class SessionEnsurer
    {
        private readonly IConsole console;
        private readonly IAccountResource resource;
        private readonly ISessionManager session;
        private readonly RemoteRenderer remoteRenderer;

        public SessionEnsurer(IConsole console, IAccountResource resource, ISessionManager session)
        {
            this.console = console;
            this.resource = resource;
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
                return await session.CreateAndSaveAsync(resource, userName, password, cancellation);
            }

            if (session.Current.IsExpired)
            {
                console.WriteLine($"Your session for '{session.Alias}' is expired. " +
                    $"The {RpaCommand.CommandName} tool will try to log in again with user={session.Current.UserName} to:");
                remoteRenderer.Render(session.Remote);
                console.Write($"Please provide the password: ");
                var password = console.Password();
                return await session.RenewAndSaveAsync(resource, password, cancellation);
            }

            return session.Current;
        }
    }
}