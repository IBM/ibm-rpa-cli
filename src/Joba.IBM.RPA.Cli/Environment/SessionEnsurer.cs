namespace Joba.IBM.RPA.Cli
{
    class SessionEnsurer
    {
        private readonly IAccountResource resource;
        private readonly ISessionManager session;
        private readonly RemoteRenderer remoteRenderer;

        public SessionEnsurer(IAccountResource resource, ISessionManager session)
        {
            this.resource = resource;
            this.session = session;
            remoteRenderer = new RemoteRenderer(session.Alias, 2);
        }

        public async Task<Session> EnsureAsync(CancellationToken cancellation)
        {
            if (session.Current == Session.NoSession)
            {
                ExtendedConsole.WriteLine($"You need to provide credentials for:");
                remoteRenderer.Render(session.Remote);
                Console.Write("User name: ");
                var userName = Console.ReadLine() ?? throw new OperationCanceledException("User did not provide the 'user name'");
                Console.Write("Password: ");
                var password = ExtendedConsole.Password();
                return await session.CreateAndSaveAsync(resource, userName, password, cancellation);
            }

            if (session.Current.IsExpired)
            {
                ExtendedConsole.WriteLine($"Your session for '{session.Alias}' is expired. " +
                    $"The {RpaCommand.CommandName} tool will try to log in again with user={session.Current.UserName} to:");
                remoteRenderer.Render(session.Remote);
                ExtendedConsole.Write($"Please provide the password: ");
                var password = ExtendedConsole.Password();
                return await session.RenewAndSaveAsync(resource, password, cancellation);
            }

            return session.Current;
        }
    }
}