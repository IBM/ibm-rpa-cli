namespace Joba.IBM.RPA
{
    class WdgAuthenticator : IAccountAuthenticator
    {
        private readonly IAccountResource resource;

        public WdgAuthenticator(IAccountResource resource) => this.resource = resource;

        async Task<CreatedSession> IAccountAuthenticator.AuthenticateAsync(AccountCredentials credentials, CancellationToken cancellation) =>
            await resource.AuthenticateAsync(credentials.TenantCode, credentials.UserName, credentials.Password, cancellation);
    }
}
