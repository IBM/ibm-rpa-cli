using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    public static class RpaClientFactory
    {
        public static IRpaClient CreateFromRegion(Region region) => CreateFromAddress(region.ApiAddress);
        public static IRpaClient CreateFromAddress(Uri address) => new RpaClient(HttpRpaFactory.Create(address));

        public static IRpaClient CreateFromEnvironment(Environment environment)
        {
            var client = HttpRpaFactory.Create(environment,
                new RenewExpiredSession(CreateFromAddress(environment.Remote.Address).Account, environment));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", environment.Session.Current.Token);

            return new RpaClient(client);
        }

        class RenewExpiredSession : IRenewExpiredSession
        {
            private readonly IAccountResource resource;
            private readonly Environment environment;
            private readonly SemaphoreSlim semaphore = new(1);

            public RenewExpiredSession(IAccountResource resource, Environment environment)
            {
                this.resource = resource;
                this.environment = environment;
            }

            async Task<Session> IRenewExpiredSession.RenewAsync(CancellationToken cancellation)
            {
                try
                {
                    await semaphore.WaitAsync(cancellation);
                    if (!environment.Session.Current.IsExpired)
                        return environment.Session.Current;

                    var sessionExpiredMessage = "Your session expired.";
                    ExtendedConsole.WriteLine($"{sessionExpiredMessage:red} " +
                        $"The {RpaCommand.CommandName} tool will try to log in again using tenant={environment.Session.Current.TenantName:blue} " +
                        $"and user={environment.Session.Current.UserName}. Please provide the password:");
                    var password = ExtendedConsole.Password();
                    return await environment.Session.RenewAndSaveAsync(resource, password, cancellation);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
    }
}
