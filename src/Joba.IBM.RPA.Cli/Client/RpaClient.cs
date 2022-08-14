using System.Globalization;
using System.Net.Http.Json;

namespace Joba.IBM.RPA.Cli
{
    class RpaClient : IRpaClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        private readonly HttpClient client;

        public RpaClient(HttpClient client)
        {
            this.client = client;

            Account = new AccountClient(client);
            ScriptVersion = new ScriptVersionClient(client);
            Script = new ScriptClient(client, ScriptVersion);
        }

        public IAccountClient Account { get; }
        public IScriptClient Script { get; }
        public IScriptVersionClient ScriptVersion { get; }

        public async Task<ServerConfig> GetConfigurationAsync(CancellationToken cancellation) =>
            await client.GetFromJsonAsync<ServerConfig>($"{CultureInfo.CurrentCulture.Name}/configuration", SerializerOptions, cancellation);

        public void Dispose()
        {
            client?.Dispose();
        }

        class ScriptClient : IScriptClient
        {
            private readonly HttpClient client;
            private readonly IScriptVersionClient versionClient;

            public ScriptClient(HttpClient client, IScriptVersionClient versionClient)
            {
                this.client = client;
                this.versionClient = versionClient;
            }

            public async Task<ScriptVersion?> GetLatestVersionAsync(Guid scriptId, CancellationToken cancellation)
            {
                //v1.0/en-US/script/{id}/info
                //var url = $"{CultureInfo.CurrentCulture.Name}/script/{scriptId}/info";
                //var test = await client.GetStringAsync(aaaa, cancellation);

                var url = $"{CultureInfo.CurrentCulture.Name}/script/{scriptId}/version?offset=0&limit=1&orderBy=version&asc=false";
                //var aa = await client.GetStringAsync(url);
                var response = await client.GetFromJsonAsync<PagedResponse<ScriptVersionBuilder>>(url, SerializerOptions, cancellation);
                if (response.Results.Length == 0)
                    return null;

                var builder = response.Results[0];
                var content = await versionClient.GetContentAsync(builder.Id, cancellation);
                return builder.Build(content);
            }

            public async Task<ScriptVersion?> GetLatestVersionAsync(string scriptName, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/script?offset=0&limit=20&search={scriptName}&orderBy=lastPublishedDate&asc=desc";
                var response = await client.GetFromJsonAsync<PagedResponse<Script>>(url, SerializerOptions, cancellation);
                if (response.Results.Length == 0)
                    return null;

                var script = response.Results.FirstOrDefault(s => s.Name == scriptName);
                if (script == null)
                    return null;

                return await GetLatestVersionAsync(script.Id, cancellation);
            }

            record class Script(Guid Id, string Name);
        }

        class ScriptVersionClient : IScriptVersionClient
        {
            private readonly HttpClient client;

            public ScriptVersionClient(HttpClient client) => this.client = client;

            public async Task<string> GetContentAsync(Guid scriptVersionId, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/script-version/{scriptVersionId}/content";
                return await client.GetStringAsync(url, cancellation);
            }
        }

        class AccountClient : IAccountClient
        {
            private readonly HttpClient client;

            public AccountClient(HttpClient client) => this.client = client;

            public async Task<Session> AuthenticateAsync(int tenantCode, string userName, string password, CancellationToken cancellation)
            {
                var tenants = await FetchTenantsAsync(userName, cancellation);
                var tenant = tenants.FirstOrDefault(t => t.Code == tenantCode);
                if (tenant == null)
                    throw new Exception(BuildException(tenantCode, userName, tenants.ToArray()));

                return await AuthenticateAsync(tenant.Id, userName, password, cancellation);
            }

            private async Task<Session> AuthenticateAsync(Guid tenantId, string userName, string password, CancellationToken cancellation)
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
                return await response.Content.ReadFromJsonAsync<TokenResponse?>(SerializerOptions, cancellation)
                    ?? throw new Exception("Could not read token from http response");
            }

            private static string BuildException(int tenantCode, string userName, Tenant[] tenants)
            {
                return new StringBuilder()
                    .AppendLine($"The specified tenant code '{tenantCode}' does not exist for the user '{userName}'. Here are the available tenants:")
                    .AppendLine(string.Join(System.Environment.NewLine, tenants.Select(t => $"{t.Code} - {t.Name}")))
                    .ToString();
            }

            public async Task<IEnumerable<Tenant>> FetchTenantsAsync(string userName, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/account/tenant";
                var model = new { UserName = userName };

                var response = await client.PostAsJsonAsync(url, model, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);
                return await response.Content.ReadFromJsonAsync<Tenant[]>(SerializerOptions, cancellation)
                    ?? throw new Exception("Could not read tenants from http response");
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

        record struct PagedResponse<T>(T[] Results);

        record class ScriptVersionBuilder(Guid Id, Guid ScriptId, int Version, string ProductVersion)
        {
            public ScriptVersion Build(string content) => string.IsNullOrEmpty(content) ?
                throw new Exception($"Could not build {nameof(ScriptVersion)} because {nameof(content)} is null or empty") :
                new(Id, ScriptId, Version, System.Version.Parse(ProductVersion), content);
        }
    }
}
