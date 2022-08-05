using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;

namespace Joba.IBM.RPA
{
    internal static class HttpFactory
    {
        public static HttpClient Create(Uri address)
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
            var pollyHandler = new PolicyHttpMessageHandler(policy) { InnerHandler = new HttpClientHandler() };
            var client = new HttpClient(new UserAgentHandler(pollyHandler)) { BaseAddress = address };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}
