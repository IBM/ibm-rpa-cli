using System.Net.Http.Headers;
using System.Reflection;

namespace Joba.IBM.RPA
{
    internal class UserAgentHandler : DelegatingHandler
    {
        public UserAgentHandler(DelegatingHandler handler) : base(handler) { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var assembly = Assembly.GetEntryAssembly().GetName();
            var version = assembly.Version.ToString(3);
            var productName = assembly.Name;
            var osBitness = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            var osPlatform = Environment.OSVersion.Platform;
            var osVersion = Environment.OSVersion;
            var machineName = Environment.MachineName;

            // {executableName}/{wdgVersion} ({osVersion}; {osPlatform}; {osBitness}) ({machineName})
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue(productName, version));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue($"({osVersion}; {osPlatform}; {osBitness})"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue($"({machineName})"));

            return base.SendAsync(request, cancellationToken);
        }
    }
}
