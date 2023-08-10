using System.Net.Http.Json;

namespace Joba.IBM.RPA
{
    class RedHatOpenshiftOidcAuthenticator : IAccountAuthenticator
    {
        private readonly IRpaHttpClientFactory httpFactory;
        private readonly Region region;
        private readonly Uri cloudPakAddress;

        public RedHatOpenshiftOidcAuthenticator(IRpaHttpClientFactory httpFactory, Region region, Uri cloudPakAddress)
        {
            this.httpFactory = httpFactory;
            this.region = region;
            this.cloudPakAddress = cloudPakAddress;
        }

        /// <summary>
        /// https://www.ibm.com/docs/en/rpa/23.0?topic=call-authenticating-rpa-api#authenticating-to-the-api-through-ums-with-a-custom-identity-provider
        /// </summary>
        async Task<CreatedSession> IAccountAuthenticator.AuthenticateAsync(AccountCredentials credentials, CancellationToken cancellation)
        {
            var iamAccess = await GetIamAccessTokenAsync(credentials, cancellation);
            var (tenantId, validatedAuthToken, oidcToken) = await GetOidcTokenAsync(credentials, iamAccess, cancellation);
            return await AuthenticateAsync(tenantId, validatedAuthToken, oidcToken, cancellation);
        }

        private async Task<CreatedSession> AuthenticateAsync(Guid tenantId, string validatedAuthToken, string oidcToken, CancellationToken cancellation)
        {
            using var client = httpFactory.Create(region.ApiAddress);
            var parameters = new Dictionary<string, string> { { "grant_type", "password" } };

            var content = new FormUrlEncodedContent(parameters);
            var request = new HttpRequestMessage(HttpMethod.Post, "token") { Content = content };
            request.Headers.Add("Cookie", $"ibm-private-cloud-session={validatedAuthToken}");
            request.Headers.Add("tenantid", tenantId.ToString());
            request.Headers.Add("oidc-access-token", oidcToken);

            var response = await client.SendAsync(request, cancellation);
            await response.ThrowWhenUnsuccessfulAsync(cancellation);
            return await response.Content.ReadFromJsonAsync<CreatedSession?>(cancellationToken: cancellation)
                ?? throw new Exception("Could not read token from http response");
        }

        /// <summary>
        /// iam_access_token=$(curl -ks -H "Content-Type: application/x-www-form-urlencoded;charset=UTF-8" -d "grant_type=password&username=$USERNAME&password=$PASSWORD&scope=openid" $CP_CONSOLE/idprovider/v1/auth/identitytoken | jq -r .access_token)
        /// </summary>
        private async Task<CloudPakIdentityTokenResponse> GetIamAccessTokenAsync(AccountCredentials credentials, CancellationToken cancellation)
        {
            //In $CP_CONSOLE, enter the Cloud Pak console URL. To get this URL, follow the steps in Accessing your cluster by using the console.
            using var client = httpFactory.Create(cloudPakAddress);
            var parameters = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "scope", "openid" },
                    { "username", credentials.UserName },
                    { "password", credentials.Password },
                };
            var content = new FormUrlEncodedContent(parameters);
            var response = await client.PostAsync("idprovider/v1/auth/identitytoken", content, cancellation);
            await response.ThrowWhenUnsuccessfulAsync(cancellation);
            return await response.Content.ReadFromJsonAsync<CloudPakIdentityTokenResponse>(cancellationToken: cancellation);
        }

        private async Task<(Guid, string, string)> GetOidcTokenAsync(AccountCredentials credentials, CloudPakIdentityTokenResponse iamAccess, CancellationToken cancellation)
        {
            //In $ZENHOST, enter the IBM RPA Control Center URL without /rpa/ui or the IBM RPA API without /rpa/api.
            var address = region.ApiAddress.ToString();
            var index = address.IndexOf("/rpa/api");
            var zenhostAddress = new Uri(address[..index]);

            using var client = httpFactory.Create(zenhostAddress);
            var validateAuthResponse = await ValidateAuthenticationAsync();
            var zenTokenResponse = await GetZenTokenAsync();
            var tenant = zenTokenResponse.Tenants.FirstOrDefault(t => t.Code == credentials.TenantCode);
            if (tenant == null)
                throw new InvalidOperationException($"The Single Sign-On did not returned a tenant with code={credentials.TenantCode}. Returned tenants: {string.Join(',', zenTokenResponse.Tenants.Select(t => $"[{t.Code}] {t.Name}"))}");
            return (tenant.Id, validateAuthResponse.AccessToken, zenTokenResponse.OidcAccessToken);

            //validate_auth=$(curl -ks -H "username:$USERNAME" -H "iam-token: $iam_access_token" $ZENHOST/v1/preauth/validateAuth | jq -r .accessToken)
            async Task<CloudPakValidatedAuthResponse> ValidateAuthenticationAsync()
            {
                client.DefaultRequestHeaders.Add("iam-token", iamAccess.AccessToken);
                client.DefaultRequestHeaders.Add("username", credentials.UserName);
                var response = await client.GetAsync("v1/preauth/validateAuth", cancellation);
                await response.ThrowWhenUnsuccessfulAsync(cancellation);
                return await response.Content.ReadFromJsonAsync<CloudPakValidatedAuthResponse>(cancellationToken: cancellation);
            }

            //user_info=$(curl -ks --cookie "ibm-private-cloud-session=$validate_auth" $ZENHOST/rpa/api/zen-token-login)
            async Task<CloudPakZenTokenLoginResponse> GetZenTokenAsync()
            {
                client.DefaultRequestHeaders.Add("Cookie", $"ibm-private-cloud-session={validateAuthResponse.AccessToken}");
                var response = await client.PostAsync("rpa/api/zen-token-login", null, cancellation);
                await response.ThrowWhenUnsuccessfulAsync(cancellation);
                return await response.Content.ReadFromJsonAsync<CloudPakZenTokenLoginResponse>(cancellationToken: cancellation);
            }
        }

        record struct CloudPakIdentityTokenResponse(
            [property: JsonPropertyName("access_token")] string AccessToken,
            [property: JsonPropertyName("token_type")] string TokenType,
            [property: JsonPropertyName("expires_in")] int ExpiresIn,
            [property: JsonPropertyName("scope")] string Scope,
            [property: JsonPropertyName("refresh_token")] string RefreshToken,
            [property: JsonPropertyName("id_token")] string IdToken);

        record struct CloudPakValidatedAuthResponse(
            [property: JsonPropertyName("username")] string UserName,
            [property: JsonPropertyName("role")] string Role,
            [property: JsonPropertyName("permissions")] string[] Permissions,
            [property: JsonPropertyName("iss")] string Iss,
            [property: JsonPropertyName("aud")] string Aud,
            [property: JsonPropertyName("uid")] string Id,
            [property: JsonPropertyName("iam")] CloudPakIam Iam,
            [property: JsonPropertyName("accessToken")] string AccessToken);
        record struct CloudPakIam([property: JsonPropertyName("accessToken")] string AccessToken);

        record struct CloudPakZenTokenLoginResponse(
            [property: JsonPropertyName("oidcAccessToken")] string OidcAccessToken,
            [property: JsonPropertyName("oidcTokenId")] string OidcTokenId,
            [property: JsonPropertyName("oidcTenantName")] string OidcTenantName,
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("email")] string Email,
            [property: JsonPropertyName("tenants")] Tenant[] Tenants);
    }
}
