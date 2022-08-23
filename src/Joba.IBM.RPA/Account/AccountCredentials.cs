namespace Joba.IBM.RPA
{
    public record class AccountCredentials(int TenantCode, string UserName, string Password)
    {
        public async Task<Session> AuthenticateAsync(IAccountResource resource, CancellationToken cancellation)
        {
            return await resource.AuthenticateAsync(TenantCode, UserName, Password, cancellation);
        }
    }
}
