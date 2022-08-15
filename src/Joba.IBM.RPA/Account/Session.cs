namespace Joba.IBM.RPA
{
    public record class Session(string Token, int TenantCode, Guid TenantId, string TenantName, string PersonName);

    public interface ISession
    {
        Region Region { get; }
        string Token { get; }
    }
}
