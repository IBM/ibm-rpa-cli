using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace Joba.IBM.RPA.Cli
{
    class RpaClient : IRpaClient
    {
        internal static readonly JsonSerializerOptions SerializerOptions = CreateJsonSerializerOptions();
        internal static JsonSerializerOptions CreateJsonSerializerOptions()
        {
            var @default = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            @default.Converters.Add(new WalVersionJsonConverter());
            return @default;
        }

        private readonly HttpClient client;
        public RpaClient(HttpClient client)
        {
            this.client = client;
            if (client.BaseAddress == null)
                throw new ArgumentException($"The '{nameof(client)}' needs to have '{nameof(client.BaseAddress)}' set");

            Account = new AccountResource(client);
            ScriptVersion = new ScriptVersionResource(client);
            Script = new ScriptResource(client, ScriptVersion);
            Parameter = new ParameterResource(client);
        }

        public Uri Address => client.BaseAddress!;
        public IAccountResource Account { get; }
        public IScriptResource Script { get; }
        public IScriptVersionResource ScriptVersion { get; }
        public IParameterResource Parameter { get; }

        public async Task<ServerConfig> GetConfigurationAsync(CancellationToken cancellation) =>
            await client.GetFromJsonAsync<ServerConfig>($"{CultureInfo.CurrentCulture.Name}/configuration", SerializerOptions, cancellation);

        public void Dispose()
        {
            client?.Dispose();
        }

        class ScriptResource : IScriptResource
        {
            private readonly HttpClient client;
            private readonly IScriptVersionResource versionResource;

            public ScriptResource(HttpClient client, IScriptVersionResource versionResource)
            {
                this.client = client;
                this.versionResource = versionResource;
            }

            public async Task<ScriptVersion?> GetLatestVersionAsync(Guid scriptId, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/script/{scriptId}/version?offset=0&limit=1&orderBy=version&asc=false&include=Script";
                var response = await client.GetFromJsonAsync<PagedResponse<ScriptVersionBuilder>>(url, SerializerOptions, cancellation);
                if (response.Results.Length == 0)
                    return null;

                var builder = response.Results[0];
                var content = await versionResource.GetContentAsync(builder.Id, cancellation);
                return builder.Build(content);
            }

            public async Task<ScriptVersion?> GetLatestVersionAsync(string scriptName, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/script?offset=0&limit=20&search={scriptName}&orderBy=modificationDate&asc=false";
                var response = await client.GetFromJsonAsync<PagedResponse<Script>>(url, SerializerOptions, cancellation);
                if (response.Results.Length == 0)
                    return null;

                var script = response.Results.FirstOrDefault(s => s.Name == scriptName);
                if (script == null)
                    return null;

                return await GetLatestVersionAsync(script.Id, cancellation);
            }

            public async Task<IEnumerable<Script>> SearchAsync(string scriptName, int limit, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/script?offset=0&limit={limit}&search={scriptName}&orderBy=name&asc=true";
                var response = await client.GetFromJsonAsync<PagedResponse<Script>>(url, SerializerOptions, cancellation);
                return response.Results;
            }

            public async Task<ScriptVersion?> GetAsync(string scriptName, WalVersion version, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/script/{scriptName}/info?versionId={version}&excludeContent=false";
                var response = await client.GetAsync(url, cancellation);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;
                await response.ThrowWhenUnsuccessful(cancellation);
                var data = await response.Content.ReadFromJsonAsync<ScriptInfo>(SerializerOptions, cancellation);
                if (data == null)
                    throw new InvalidOperationException($"Could not deserialize the response from: {url}");

                return new ScriptVersion(data.ScriptVersionId, data.ScriptId, scriptName, data.Version,
                    Version.Parse(data.ProductVersion), data.Content);
            }

            public async Task<ScriptVersion> PublishAsync(PublishScript script, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/script/publish";
                var response = await client.PutAsJsonAsync(url, script, SerializerOptions, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);

                var builder = await response.Content.ReadFromJsonAsync<ScriptVersionBuilder>(SerializerOptions, cancellation);
                if (builder == null)
                    throw new InvalidOperationException($"Could not deserialize the response from: {url}");

                return builder.Build(script.Content);
            }
        }

        class ScriptInfo
        {
            public required Guid ScriptId { get; set; }
            public required Guid ScriptVersionId { get; set; }
            public required string Name { get; set; }
            public required string ProductVersion { get; set; }
            public required string Content { get; set; }
            public required WalVersion Version { get; set; }
        }

        class ScriptVersionResource : IScriptVersionResource
        {
            private readonly HttpClient client;

            public ScriptVersionResource(HttpClient client) => this.client = client;

            public async Task<string> GetContentAsync(Guid scriptVersionId, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/script-version/{scriptVersionId}/content";
                return await client.GetStringAsync(url, cancellation);
            }
        }

        /// <summary>
        /// TODO: use https://www.ibm.com/docs/en/rpa/21.0?topic=api-reference
        /// </summary>
        class AccountResource : IAccountResource
        {
            private readonly HttpClient client;

            public AccountResource(HttpClient client) => this.client = client;

            public async Task<CreatedSession> AuthenticateAsync(int tenantCode, string userName, string password, CancellationToken cancellation)
            {
                var tenants = await FetchTenantsAsync(userName, cancellation);
                var tenant = tenants.FirstOrDefault(t => t.Code == tenantCode);
                if (tenant == null)
                    throw new Exception(BuildException(tenantCode, userName, tenants.ToArray()));

                return await GetTokenAsync(tenant.Id, userName, password, cancellation);
            }

            private async Task<CreatedSession> GetTokenAsync(Guid tenantId, string userName, string password, CancellationToken cancellation)
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
                return await response.Content.ReadFromJsonAsync<CreatedSession?>(SerializerOptions, cancellation)
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
        }

        record struct PagedResponse<T>(T[] Results);

        record class ScriptVersionBuilder(Guid Id, Guid ScriptId, WalVersion Version, string ProductVersion, ScriptVersionBuilder.ScriptInfo Script)
        {
            public ScriptVersion Build(string content) => string.IsNullOrEmpty(content) ?
                throw new Exception($"Could not build {nameof(ScriptVersion)} because {nameof(content)} is null or empty") :
                new(Id, ScriptId, Script.Name, Version, System.Version.Parse(ProductVersion), content);

            public record class ScriptInfo(Guid Id, string Name);
        }

        class ParameterResource : IParameterResource
        {
            private readonly HttpClient client;

            public ParameterResource(HttpClient client) => this.client = client;

            public async Task<IEnumerable<Parameter>> SearchAsync(string parameterName, int limit, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/parameter?offset=0&limit=50&search={parameterName}&orderBy=id&asc=true";
                var response = await client.GetFromJsonAsync<PagedResponse<Parameter>>(url, SerializerOptions, cancellation);
                return response.Results;
            }

            public async Task<IEnumerable<Parameter>> GetAsync(string[] parameters, CancellationToken cancellation)
            {
                if (parameters == null || parameters.Length == 0)
                    throw new ArgumentException("Parameters cannot be an empty array", nameof(parameters));

                var ids = string.Join(", ", parameters);
                var url = $"{CultureInfo.CurrentCulture.Name}/parameter/values?ids={Uri.EscapeDataString(ids)}";
                var response = await client.GetFromJsonAsync<IEnumerable<Parameter>>(url, SerializerOptions, cancellation);
                return response ?? throw new Exception("Could not convert the response");
            }

            public async Task<Parameter?> GetAsync(string parameterName, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/parameter/value";
                var body = new { Id = parameterName };
                var response = await client.PostAsJsonAsync(url, body, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);
                return await response.Content.ReadFromJsonAsync<Parameter>(SerializerOptions, cancellation);
            }
        }
    }
}
