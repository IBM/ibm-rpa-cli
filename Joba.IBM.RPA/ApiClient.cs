using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Joba.IBM.RPA
{
    internal class ApiClient : IDisposable
    {
        private readonly HttpClient client;

        public ApiClient(HttpClient client)
        {
            this.client = client;
        }

        public async Task<Session> AuthenticateAsync(Account account, CancellationToken cancellation)
        {
            var tenant = await FetchTenantAsync(account, cancellation);
            var token = await GetTokenAsync(account, tenant, cancellation);
            return new Session(token.Token, tenant.Code, tenant.Id, tenant.Name, token.Name);
        }

        private async Task<TokenResponse> GetTokenAsync(Account account, Tenant tenant, CancellationToken cancellation)
        {
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", account.UserName },
                { "password", account.Password },
                { "culture", CultureInfo.CurrentCulture.Name },
            };

            var content = new FormUrlEncodedContent(parameters);
            var request = new HttpRequestMessage(HttpMethod.Post, "token") { Content = content };
            request.Headers.Add("tenantid", tenant.Id.ToString());
            var response = await client.SendAsync(request, cancellation);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TokenResponse>(Constants.SerializerOptions, cancellation);
        }

        private async Task<Tenant> FetchTenantAsync(Account account, CancellationToken cancellation)
        {
            var url = $"{CultureInfo.CurrentCulture.Name}/account/tenant";
            var model = new { UserName = account.UserName };

            var response = await client.PostAsJsonAsync(url, model, cancellation);
            response.EnsureSuccessStatusCode();
            var tenants = await response.Content.ReadFromJsonAsync<Tenant[]>(Constants.SerializerOptions, cancellation);
            var tenant = tenants.FirstOrDefault(t => t.Code == account.TenantCode);
            if (tenant == null)
                throw new Exception(BuildException(account, tenants));

            return tenant;
        }

        private static string BuildException(Account account, Tenant[] tenants)
        {
            return new StringBuilder()
                .AppendLine($"The specified tenant code '{account.TenantCode}' does not exist for the user '{account.UserName}'. Here are the available tenants:")
                .AppendLine(string.Join(Environment.NewLine, tenants.Select(t => $"{t.Code} - {t.Name}")))
                .ToString();
        }

        void IDisposable.Dispose()
        {
            client?.Dispose();
        }

        record class Tenant(Guid Id, int Code, string Name);
        struct TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string Token { get; init; }
            public string Name { get; init; }
        }
    }
}
