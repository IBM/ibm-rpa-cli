namespace Joba.IBM.RPA
{
    public interface IRpaClient : IDisposable
    {
        Uri Address { get; }
        Task<ServerConfig> GetConfigurationAsync(CancellationToken cancellation);
        IAccountResource Account { get; }
        IScriptResource Script { get; }
        IParameterResource Parameter { get; }
    }

    public interface IRpaClientFactory
    {
        IRpaClient CreateFromAddress(Uri address);
        IRpaClient CreateFromRegion(Region region);
        IRpaClient CreateFromEnvironment(Environment environment);
        IRpaClient CreateFromPackageSource(PackageSource source);
    }

    public interface IAccountResource
    {
        Task<IEnumerable<Tenant>> FetchTenantsAsync(string userName, CancellationToken cancellation);
        Task<CreatedSession> AuthenticateAsync(int tenantCode, string userName, string password, CancellationToken cancellation);
    }

    public interface IScriptResource
    {
        Task<ScriptVersion?> GetLatestVersionAsync(Guid scriptId, CancellationToken cancellation);
        Task<ScriptVersion?> GetLatestVersionAsync(string scriptName, CancellationToken cancellation);
        /// <summary>
        /// TODO: build a local Cache decorator for this, for X minutes.
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="limit"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        Task<IEnumerable<Script>> SearchAsync(string scriptName, int limit, CancellationToken cancellation);
        Task<ScriptVersion?> GetAsync(string scriptName, WalVersion version, CancellationToken cancellation);
        Task<ScriptVersion> PublishAsync(PublishScript script, CancellationToken cancellation);
    }

    public interface IScriptVersionResource
    {
        /// <summary>
        /// TODO: build a local Cache decorator for this.
        /// </summary>
        /// <param name="scriptVersionId"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        Task<string> GetContentAsync(Guid scriptVersionId, CancellationToken cancellation);
    }

    public interface IParameterResource
    {
        Task<IEnumerable<Parameter>> SearchAsync(string parameterName, int limit, CancellationToken cancellation);
        Task<Parameter?> GetAsync(string parameterName, CancellationToken cancellation);
        Task<IEnumerable<Parameter>> GetAsync(string[] parameters, CancellationToken cancellation);
        Task<Parameter> CreateAsync(string parameterName, string value, CancellationToken cancellation);
        Task<Parameter> UpdateAsync(string parameterName, string value, CancellationToken cancellation);
        Task<Parameter> CreateOrUpdateAsync(string parameterName, string value, CancellationToken cancellation);
    }

    public record class Parameter([property: JsonPropertyName("Id")] string Name, string Value);
    public record class PublishScript(Guid? Id, Guid? VersionId, string Name, string? Description, string Content, string ProductVersion,
        bool SetAsProduction, int GreenExecutionTimeSeconds, int YellowExecutionTimeSeconds, int RedExecutionTimeSeconds);
}
