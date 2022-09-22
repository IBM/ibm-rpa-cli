using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    public class RpaClientFactory : IRpaClientFactory
    {
        private readonly IConsole console;

        public RpaClientFactory(IConsole console) => this.console = console;

        public IRpaClient CreateFromAddress(Uri address) => new RpaClient(HttpRpaFactory.Create(address));
        IRpaClient IRpaClientFactory.CreateFromRegion(Region region) => ((IRpaClientFactory)this).CreateFromAddress(region.ApiAddress);

        IRpaClient IRpaClientFactory.CreateFromPackageSource(PackageSource source)
        {
            var sessionEnsurer = new SessionEnsurer(console, CreateFromAddress(source.Remote.Address).Account, source.Session);
            var client = HttpRpaFactory.Create(source.Remote.Address, new RenewExpiredSession(sessionEnsurer));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", source.Session.Current.Token);
            return new RpaClient(client);
        }

        IRpaClient IRpaClientFactory.CreateFromEnvironment(Environment environment)
        {
            var sessionEnsurer = new SessionEnsurer(console, CreateFromAddress(environment.Remote.Address).Account, environment.Session);
            var client = HttpRpaFactory.Create(environment.Remote.Address, new RenewExpiredSession(sessionEnsurer));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", environment.Session.Current.Token);
            return new RpaClient(client);
        }

        class RenewExpiredSession : IRenewExpiredSession
        {
            private readonly SessionEnsurer sessionEnsurer;
            private readonly SemaphoreSlim semaphore = new(1);

            public RenewExpiredSession(SessionEnsurer sessionEnsurer)
            {
                this.sessionEnsurer = sessionEnsurer;
            }

            async Task<Session> IRenewExpiredSession.RenewAsync(CancellationToken cancellation)
            {
                try
                {
                    await semaphore.WaitAsync(cancellation);
                    return await sessionEnsurer.EnsureAsync(cancellation);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
    }
}
