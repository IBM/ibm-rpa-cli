using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    public class RpaClientFactory : IRpaClientFactory
    {
        IRpaClient IRpaClientFactory.CreateFromAddress(Uri address) => new RpaClient(HttpRpaFactory.Create(address));
        IRpaClient IRpaClientFactory.CreateFromRegion(Region region) => ((IRpaClientFactory)this).CreateFromAddress(region.ApiAddress);

        IRpaClient IRpaClientFactory.CreateFromPackageSource(PackageSource source)
        {
            var sessionEnsurer = new SessionEnsurer(CreateFromAddress(source.Remote.Address).Account, source.Session);
            var client = HttpRpaFactory.Create(source.Remote.Address, new RenewExpiredSession(sessionEnsurer));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", source.Session.Current.Token);
            return new RpaClient(client);
        }

        IRpaClient IRpaClientFactory.CreateFromEnvironment(Environment environment)
        {
            var sessionEnsurer = new SessionEnsurer(CreateFromAddress(environment.Remote.Address).Account, environment.Session);
            var client = HttpRpaFactory.Create(environment.Remote.Address, new RenewExpiredSession(sessionEnsurer));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", environment.Session.Current.Token);
            return new RpaClient(client);
        }

        public static IRpaClient CreateFromRegion(Region region)
        {
            IRpaClientFactory factory = new RpaClientFactory();
            return factory.CreateFromRegion(region);
        }

        public static IRpaClient CreateFromAddress(Uri address)
        {
            IRpaClientFactory factory = new RpaClientFactory();
            return factory.CreateFromAddress(address);
        }

        public static IRpaClient CreateFromEnvironment(Environment environment)
        {
            IRpaClientFactory factory = new RpaClientFactory();
            return factory.CreateFromEnvironment(environment);
        }

        public static IRpaClient CreateFromPackageSource(PackageSource source)
        {
            IRpaClientFactory factory = new RpaClientFactory();
            return factory.CreateFromPackageSource(source);
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
