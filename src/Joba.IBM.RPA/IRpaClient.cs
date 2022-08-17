namespace Joba.IBM.RPA
{
    public interface IRpaClient : IDisposable
    {
        Uri Address { get; }
        Task<ServerConfig> GetConfigurationAsync(CancellationToken cancellation);
        IAccountClient Account { get; }
        IScriptClient Script { get; }
    }

    public interface IAccountClient
    {
        Task<IEnumerable<Tenant>> FetchTenantsAsync(string userName, CancellationToken cancellation);
        Task<Session> AuthenticateAsync(int tenantCode, string userName, string password, CancellationToken cancellation);
    }

    public interface IScriptClient
    {
        Task<ScriptVersion?> GetLatestVersionAsync(Guid scriptId, CancellationToken cancellation);
        Task<ScriptVersion?> GetLatestVersionAsync(string scriptName, CancellationToken cancellation);
        Task<IEnumerable<Script>> SearchAsync(string scriptName, int limit, CancellationToken cancellation);
    }

    public interface IScriptVersionClient
    {
        Task<string> GetContentAsync(Guid scriptVersionId, CancellationToken cancellation);
    }
}
