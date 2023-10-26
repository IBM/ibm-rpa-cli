namespace Joba.IBM.RPA
{
    public interface IAccountAuthenticator
    {
        Task<CreatedSession> AuthenticateAsync(AccountCredentials credentials, CancellationToken cancellation);
    }

    public interface IAccountAuthenticatorFactory
    {
        IAccountAuthenticator Create(DeploymentOption deployment, AuthenticationMethod authenticationMethod, Region region, PropertyOptions properties);
    }
}
