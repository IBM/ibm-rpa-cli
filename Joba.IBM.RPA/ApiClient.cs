using System.Globalization;
using System.Net.Http.Json;
using System.Text;
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

        public async Task<ServerConfig> GetConfigurationAsync(CancellationToken cancellation)
        {
            return await client.GetFromJsonAsync<ServerConfig>("en/configuration", Constants.SerializerOptions, cancellation);
        }

        public async Task<Session> AuthenticateAsync(int tenantCode, string userName, string password, CancellationToken cancellation)
        {
            var tenants = await FetchTenantsAsync(userName, cancellation);
            var tenant = tenants.FirstOrDefault(t => t.Code == tenantCode);
            if (tenant == null)
                throw new Exception(BuildException(tenantCode, userName, tenants.ToArray()));

            return await AuthenticateAsync(tenant.Id, userName, password, cancellation);
        }

        public async Task<Session> AuthenticateAsync(Guid tenantId, string userName, string password, CancellationToken cancellation)
        {
            var token = await GetTokenAsync(tenantId, userName, password, cancellation);
            return new Session(token.Token, token.TenantCode, tenantId, token.TenantName, token.PersonName);
        }

        private async Task<TokenResponse> GetTokenAsync(Guid tenantId, string userName, string password, CancellationToken cancellation)
        {
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", userName },
                { "password", password },
                { "culture", CultureInfo.CurrentCulture.Name },
            };

            var content = new FormUrlEncodedContent(parameters);
            var request = new HttpRequestMessage(HttpMethod.Post, "token") { Content = content };
            request.Headers.Add("tenantid", tenantId.ToString());
            var response = await client.SendAsync(request, cancellation);
            await response.ThrowWhenUnsuccessful(cancellation);
            return await response.Content.ReadFromJsonAsync<TokenResponse>(Constants.SerializerOptions, cancellation);
        }

        public async Task<IEnumerable<Tenant>> FetchTenantsAsync(string userName, CancellationToken cancellation)
        {
            var url = $"{CultureInfo.CurrentCulture.Name}/account/tenant";
            var model = new { UserName = userName };

            var response = await client.PostAsJsonAsync(url, model, cancellation);
            await response.ThrowWhenUnsuccessful(cancellation);
            return await response.Content.ReadFromJsonAsync<Tenant[]>(Constants.SerializerOptions, cancellation);
        }

        private static string BuildException(int tenantCode, string userName, Tenant[] tenants)
        {
            return new StringBuilder()
                .AppendLine($"The specified tenant code '{tenantCode}' does not exist for the user '{userName}'. Here are the available tenants:")
                .AppendLine(string.Join(Environment.NewLine, tenants.Select(t => $"{t.Code} - {t.Name}")))
                .ToString();
        }

        public void Dispose()
        {
            client?.Dispose();
        }

        struct TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string Token { get; init; }
            [JsonPropertyName("name")]
            public string PersonName { get; init; }
            public int TenantCode { get; init; }
            public string TenantName { get; init; }
        }
    }
}
