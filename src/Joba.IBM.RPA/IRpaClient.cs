using Joba.IBM.RPA.Server;

namespace Joba.IBM.RPA
{
    public interface IRpaClient : IDisposable
    {
        Uri Address { get; }
        Task<ServerConfig> GetConfigurationAsync(CancellationToken cancellation);
        IAccountResource Account { get; }
        IScriptResource Script { get; }
        IParameterResource Parameter { get; }
        IProjectResource Project { get; }
        IBotResource Bot { get; }
        IComputerGroupResource ComputerGroup { get; }
        IComputerResource Computer { get; }
        IChatResource Chat { get; }
        IChatMappingResource ChatMapping { get; }
    }

    public interface IRpaClientFactory
    {
        IRpaClient CreateFromAddress(Uri address);
        IRpaClient CreateFromRegion(Region region);
        IRpaClient CreateFromEnvironment(Environment environment);
        IRpaClient CreateFromPackageSource(PackageSource source);
    }

    public interface IRpaHttpClientFactory
    {
        HttpClient Create(Uri address);
        HttpClient Create(Uri address, IRenewExpiredSession sessionRenewal);
    }

    public interface IRenewExpiredSession
    {
        Task<Session> RenewAsync(CancellationToken cancellation);
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

    public interface IProjectResource
    {
        Task<Server.Project> CreateOrUpdateAsync(string name, string description, CancellationToken cancellation);
    }

    public interface IBotResource
    {
        Task CreateOrUpdateAsync(CreateBotRequest bot, CancellationToken cancellation);
    }

    public interface IComputerGroupResource
    {
        Task<ComputerGroup> GetAsync(string name, CancellationToken cancellation);
    }

    public interface IComputerResource
    {
        Task<IEnumerable<Computer>> SearchAsync(string? name, int limit, CancellationToken cancellation);
    }

    public interface IChatMappingResource
    {
        Task CreateOrUpdateAsync(CreateChatMappingRequest mapping, CancellationToken cancellation);
    }

    public interface IChatResource
    {
        Task<IEnumerable<Chat>> GetAllAsync(CancellationToken cancellation);
    }
}
