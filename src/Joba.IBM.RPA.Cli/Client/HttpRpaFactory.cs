using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    internal static class HttpRpaFactory
    {
        public static HttpClient Create(Uri address)
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
            var pollyHandler = new PolicyHttpMessageHandler(policy) { InnerHandler = new HttpClientHandler() };
            var userAgentHandler = new UserAgentHandler(pollyHandler);
            var client = new HttpClient(userAgentHandler) { BaseAddress = address };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        public static HttpClient Create(RemoteSettings remote, IRenewExpiredSession sessionRenewal)
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
            var pollyHandler = new PolicyHttpMessageHandler(policy) { InnerHandler = new HttpClientHandler() };
            var userAgentHandler = new UserAgentHandler(pollyHandler);
            var tokenHandler = new RefreshTokenHandler(sessionRenewal, userAgentHandler);
            var client = new HttpClient(tokenHandler) { BaseAddress = remote.Address };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        class RefreshTokenHandler : DelegatingHandler
        {
            private readonly IRenewExpiredSession sessionRenewal;

            public RefreshTokenHandler(IRenewExpiredSession sessionRenewal, DelegatingHandler innerHandler)
                : base(innerHandler)
            {
                this.sessionRenewal = sessionRenewal;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                var response = await base.SendAsync(request, cancellation);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                    return response;

                var session = await sessionRenewal.RenewAsync(cancellation);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
                return await base.SendAsync(request, cancellation);
            }
        }
    }

    internal interface IRenewExpiredSession
    {
        Task<Session> RenewAsync(CancellationToken cancellation);
    }
}
