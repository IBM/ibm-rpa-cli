using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    public static class RpaClientFactory
    {
        public static IRpaClient CreateClient(Region region) => CreateClient(region.ApiUrl);
        public static IRpaClient CreateClient(Uri address) => new RpaClient(HttpRpaFactory.Create(address));

        public static IRpaClient CreateClient(Environment environment)
        {
            var session = environment.GetSession();
            var client = HttpRpaFactory.Create(environment, CreateSession);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

            return new RpaClient(client);
        }

        private static async Task<Session> CreateSession(Environment environment, CancellationToken cancellation)
        {
            var settings = environment.Remote;
            var sessionExpiredMessage = "Your session expired.";
            ExtendedConsole.WriteLine($"{sessionExpiredMessage:red} " +
                $"The {RpaCommand.CommandName} tool will try to log in again using tenant={settings.TenantName:blue} and user={settings.UserName}. " +
                $"Please provide the password:");
            var password = ExtendedConsole.Password();
            var client = CreateClient(settings.Address);
            var session = await client.Account.AuthenticateAsync(settings.TenantCode, settings.UserName, password, cancellation);

            environment.SetSession(session);
            await environment.SaveAsync(cancellation);

            return session;
        }
    }
}
