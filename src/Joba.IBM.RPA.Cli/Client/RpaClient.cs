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
            Project = new ProjectResource(client);
            Bot = new BotResource(client);
            ComputerGroup = new ComputerGroupResource(client);
        }

        public Uri Address => client.BaseAddress!;
        public IAccountResource Account { get; }
        public IScriptResource Script { get; }
        public IScriptVersionResource ScriptVersion { get; }
        public IParameterResource Parameter { get; }
        public IProjectResource Project { get; }
        public IBotResource Bot { get; }
        public IComputerGroupResource ComputerGroup { get; }

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
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                await response.ThrowWhenUnsuccessful(cancellation);
                var value = await response.Content.ReadAsStringAsync(cancellation);
                return new Parameter(parameterName, value);
            }

            public async Task<Parameter> CreateAsync(string parameterName, string value, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/parameter";
                var parameter = new Parameter(parameterName, value);
                var response = await client.PostAsJsonAsync(url, parameter, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);
                return await response.Content.ReadFromJsonAsync<Parameter>(SerializerOptions, cancellation) ?? throw new Exception("Could not convert the response");
            }

            public async Task<Parameter> UpdateAsync(string parameterName, string value, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/parameter";
                var parameter = new Parameter(parameterName, value);
                var response = await client.PutAsJsonAsync(url, parameter, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);
                return await response.Content.ReadFromJsonAsync<Parameter>(SerializerOptions, cancellation) ?? throw new Exception("Could not convert the response"); ;
            }

            public async Task<Parameter> CreateOrUpdateAsync(string parameterName, string value, CancellationToken cancellation)
            {
                var parameter = await GetAsync(parameterName, cancellation);
                if (parameter == null)
                    return await CreateAsync(parameterName, value, cancellation);

                return await UpdateAsync(parameterName, value, cancellation);
            }
        }

        class ProjectResource : IProjectResource
        {
            private readonly HttpClient client;

            public ProjectResource(HttpClient client) => this.client = client;

            public async Task<ServerProject> CreateOrUpdateAsync(string name, string description, CancellationToken cancellation)
            {
                var project = (await SearchAsync(name, 1, cancellation)).FirstOrDefault();
                if (project == null)
                    return await CreateAsync(name, name.Replace(" ", "_"), description, cancellation);

                if (project.Name != name || project.Description != description)
                    return await UpdateAsync(project.Id, name, description, cancellation);

                return project;
            }

            private async Task<IEnumerable<ServerProject>> SearchAsync(string name, int limit, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/project?offset=0&limit={limit}&search={name}&orderBy=name&asc=true";
                var response = await client.GetFromJsonAsync<PagedResponse<ServerProject>>(url, SerializerOptions, cancellation);
                return response.Results;
            }

            private async Task<ServerProject> CreateAsync(string name, string uniqueId, string description, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/project";
                var project = new { Name = name, Description = description, TechnicalName = uniqueId };
                var response = await client.PostAsJsonAsync(url, project, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);
                return await response.Content.ReadFromJsonAsync<ServerProject>(SerializerOptions, cancellation) ?? throw new Exception("Could not convert the response");
            }

            private async Task<ServerProject> UpdateAsync(Guid id, string name, string description, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/project/{id}";
                var project = new { Name = name, Description = description };
                var response = await client.PutAsJsonAsync(url, project, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);
                return await response.Content.ReadFromJsonAsync<ServerProject>(SerializerOptions, cancellation) ?? throw new Exception("Could not convert the response");
            }
        }

        class BotResource : IBotResource
        {
            private readonly HttpClient client;

            public BotResource(HttpClient client) => this.client = client;

            public async Task CreateOrUpdateAsync(ServerBot bot, CancellationToken cancellation)
            {
                var server = (await SearchAsync(bot.ProjectId, bot.Name, 1, cancellation)).FirstOrDefault();
                if (server == null)
                    _ = await CreateAsync(bot, cancellation);
                else
                    _ = await UpdateAsync(server.Id, ServerBot.Copy(bot, server.UniqueId), cancellation);
            }

            private async Task<ServerBotSearch> UpdateAsync(Guid id, ServerBot bot, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/project/bot/{id}";
                var response = await client.PutAsJsonAsync(url, bot, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);
                return await response.Content.ReadFromJsonAsync<ServerBotSearch>(SerializerOptions, cancellation) ?? throw new Exception("Could not convert the response");
            }

            private async Task<ServerBotSearch> CreateAsync(ServerBot bot, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/project/bot";
                var response = await client.PostAsJsonAsync(url, bot, cancellation);
                await response.ThrowWhenUnsuccessful(cancellation);
                return await response.Content.ReadFromJsonAsync<ServerBotSearch>(SerializerOptions, cancellation) ?? throw new Exception("Could not convert the response");
            }

            private async Task<IEnumerable<ServerBotSearch>> SearchAsync(Guid projectId, string name, int limit, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/project/bot?projectId={projectId}&include=ScriptVersion&offset=0&limit={limit}&search={name}&orderBy=name&asc=true";
                var response = await client.GetFromJsonAsync<PagedResponse<ServerBotSearch>>(url, SerializerOptions, cancellation);
                return response.Results;
            }

            record ServerBotSearch(Guid Id, string Name, string Description, [property: JsonPropertyName("TechnicalName")] string UniqueId, ScriptVersion ScriptVersion);
        }

        class ComputerGroupResource : IComputerGroupResource
        {
            private readonly HttpClient client;

            public ComputerGroupResource(HttpClient client) => this.client = client;

            public async Task<ComputerGroup> GetAsync(string name, CancellationToken cancellation)
            {
                var computerGroup = (await SearchAsync(name, 1, cancellation)).FirstOrDefault();
                return computerGroup ?? throw new Exception($"Could not find a computer group named '{name}'");
            }

            private async Task<IEnumerable<ComputerGroup>> SearchAsync(string name, int limit, CancellationToken cancellation)
            {
                var url = $"{CultureInfo.CurrentCulture.Name}/group?offset=0&limit={limit}&search={name}&orderBy=name&asc=true";
                var response = await client.GetFromJsonAsync<PagedResponse<ComputerGroup>>(url, SerializerOptions, cancellation);
                return response.Results;
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

        class ScriptInfo
        {
            public required Guid ScriptId { get; set; }
            public required Guid ScriptVersionId { get; set; }
            public required string Name { get; set; }
            public required string ProductVersion { get; set; }
            public required string Content { get; set; }
            public required WalVersion Version { get; set; }
        }
    }
}
