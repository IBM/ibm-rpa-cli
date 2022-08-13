namespace Joba.IBM.RPA
{
    public record class Session(string Token, int TenantCode, Guid TenantId, string TenantName, string PersonName);
}
