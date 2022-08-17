namespace Joba.IBM.RPA
{
    public record class AccountCredentials(int TenantCode, string UserName, string Password)
    {
        public async Task<Session> AuthenticateAsync(IAccountClient client, CancellationToken cancellation)
        {
            return await client.AuthenticateAsync(TenantCode, UserName, Password, cancellation);
        }
    }
}
