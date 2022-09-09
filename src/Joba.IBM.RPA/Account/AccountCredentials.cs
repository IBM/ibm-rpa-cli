namespace Joba.IBM.RPA
{
    public record class AccountCredentials(int TenantCode, string UserName, string Password)
    {
        internal async Task<CreatedSession> AuthenticateAsync(IAccountResource resource, CancellationToken cancellation)
        {
            return await resource.AuthenticateAsync(TenantCode, UserName, Password, cancellation);
        }
    }
}
