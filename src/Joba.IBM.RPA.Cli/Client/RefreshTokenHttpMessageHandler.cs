using System.Net;
using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    internal class RefreshTokenHttpMessageHandler : DelegatingHandler
    {
        private readonly IRenewExpiredSession sessionRenewal;

        public RefreshTokenHttpMessageHandler(IRenewExpiredSession sessionRenewal, HttpMessageHandler innerHandler)
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
