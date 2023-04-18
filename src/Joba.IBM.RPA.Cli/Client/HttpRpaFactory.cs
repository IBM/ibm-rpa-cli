using Microsoft.Extensions.Http;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    internal static class HttpRpaFactory
    {
        private const int MaxParallelism = 10;

        public static HttpClient Create(Uri address)
        {
            var handler = new ThrottlingHttpMessageHandler(MaxParallelism, CreateUserAgentHandler());
            var client = new HttpClient(handler) { BaseAddress = address };
            ApplyDefaultRequestHeaders(client);
            return client;
        }

        public static HttpClient Create(Uri address, IRenewExpiredSession sessionRenewal)
        {
            var refreshTokenHandler = new RefreshTokenHttpMessageHandler(sessionRenewal, CreateUserAgentHandler());
            var handler = new ThrottlingHttpMessageHandler(MaxParallelism, refreshTokenHandler);
            var client = new HttpClient(handler) { BaseAddress = address };
            ApplyDefaultRequestHeaders(client);
            return client;
        }

        private static void ApplyDefaultRequestHeaders(HttpClient client)
        {
            //TODO: invalid formats
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(RpaCommand.CommandName, RpaCommand.AssemblyVersion));
            //client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(System.Environment.OSVersion.ToString()));
            //client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(System.Environment.MachineName));
        }

        private static HttpMessageHandler CreatePolicyHandler()
        {
            var policy = HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(2), 5));
            return new PolicyHttpMessageHandler(policy) { InnerHandler = new HttpClientHandler() };
        }

        private static HttpMessageHandler CreateUserAgentHandler()
        {
            var pollyHandler = CreatePolicyHandler();
            return new UserAgentHttpMessageHandler(pollyHandler);
        }
    }
}
