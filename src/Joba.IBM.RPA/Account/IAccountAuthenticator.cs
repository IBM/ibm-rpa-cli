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

    public class AccountAuthenticatorFactory : IAccountAuthenticatorFactory
    {
        private readonly IRpaClientFactory clientFactory;
        private readonly IRpaHttpClientFactory httpFactory;

        public AccountAuthenticatorFactory(IRpaClientFactory clientFactory, IRpaHttpClientFactory httpFactory)
        {
            this.clientFactory = clientFactory;
            this.httpFactory = httpFactory;
        }

        IAccountAuthenticator IAccountAuthenticatorFactory.Create(DeploymentOption deployment, AuthenticationMethod authenticationMethod, Region region, PropertyOptions properties)
        {
            if (deployment == DeploymentOption.OCP && authenticationMethod == AuthenticationMethod.OIDC)
            {
                var cloudPakAddress = properties[PropertyOptions.CloudPakConsoleAddress];
                return cloudPakAddress == null
                    ? throw new NotSupportedException($"Since the server is deployed in the CloudPak cluster, the CloudPak Console URL is required and it was not provided. More information https://ibm.github.io/ibm-rpa-cli/#/guide/environment?id=red-hat®-openshift®-support-with-single-sign-on.")
                    : (IAccountAuthenticator)new RedHatOpenshiftOidcAuthenticator(httpFactory, region, new Uri(cloudPakAddress));
            }

            var client = clientFactory.CreateFromRegion(region);
            return new WdgAuthenticator(client.Account);
        }
    }
}
