using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    public static class HttpRpaFactory
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

        public static HttpClient Create(Environment environment, Func<Environment, CancellationToken, Task<Session>> sessionFactory)
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
            var pollyHandler = new PolicyHttpMessageHandler(policy) { InnerHandler = new HttpClientHandler() };
            var userAgentHandler = new UserAgentHandler(pollyHandler);
            var tokenHandler = new RefreshTokenHandler(environment, sessionFactory, userAgentHandler);
            var client = new HttpClient(tokenHandler) { BaseAddress = environment.Remote.Address };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        class RefreshTokenHandler : DelegatingHandler
        {
            private readonly Func<Environment, CancellationToken, Task<Session>> sessionFactory;
            private readonly Environment environment;

            public RefreshTokenHandler(Environment environment, Func<Environment, CancellationToken, Task<Session>> sessionFactory, DelegatingHandler innerHandler)
                : base(innerHandler)
            {
                this.sessionFactory = sessionFactory;
                this.environment = environment;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                var response = await base.SendAsync(request, cancellation);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                    return response;

                var session = await sessionFactory.Invoke(environment, cancellation);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
                return await base.SendAsync(request, cancellation);
            }
        }
    }
}
