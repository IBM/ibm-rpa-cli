using System.Net.Http.Headers;
using System.Reflection;

namespace Joba.IBM.RPA.Cli
{
    internal class UserAgentHttpMessageHandler : DelegatingHandler
    {
        public UserAgentHttpMessageHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var assembly = Assembly.GetEntryAssembly()?.GetName() ?? throw new Exception("Could not get assembly name");
            var version = assembly.Version?.ToString(3) ?? "unknown";
            var productName = assembly.Name ?? "unknown";
            var osBitness = System.Environment.Is64BitOperatingSystem ? "x64" : "x86";
            var osPlatform = System.Environment.OSVersion.Platform;
            var osVersion = System.Environment.OSVersion;
            var machineName = System.Environment.MachineName;

            // {executableName}/{wdgVersion} ({osVersion}; {osPlatform}; {osBitness}) ({machineName})
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(productName, version));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue($"({osVersion}; {osPlatform}; {osBitness})"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue($"({machineName})"));

            return base.SendAsync(request, cancellationToken);
        }
    }
}
