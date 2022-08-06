namespace Joba.IBM.RPA
{
    internal record class Session(string Token, int TenantCode, Guid TenantId, string TenantName, string PersonName);
}
