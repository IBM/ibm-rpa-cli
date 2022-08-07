
namespace Joba.IBM.RPA
{
    public interface IHttpFactory
    {

    }

    public interface IRpaClient : IDisposable
    {
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
        Task<string> GetContentAsync(Guid scriptVersionId, CancellationToken cancellation);
    }

    public record class ScriptVersion(Guid Id, Guid ScriptId, int Version, string ProductVersion);
}
