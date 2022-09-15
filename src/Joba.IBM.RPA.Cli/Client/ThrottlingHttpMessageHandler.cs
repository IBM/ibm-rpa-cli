namespace Joba.IBM.RPA.Cli
{
    internal class ThrottlingHttpMessageHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim throttler;

        public ThrottlingHttpMessageHandler(int maxParallelism, HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            throttler = new SemaphoreSlim(maxParallelism);
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await throttler.WaitAsync(cancellationToken);

            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (response.Headers.TryGetValues("X-Ratelimit-Limit", out var limit))
                {
                    if (response.Headers.TryGetValues("X-Ratelimit-Reset", out var values))
                    {
                        var unixTime = double.Parse(values.First());
                        var time = UnixTimeStampToDateTime(unixTime).AddSeconds(5);
                        var delay = time - DateTime.UtcNow;
                        await Task.Delay(delay, cancellationToken);
                        return await base.SendAsync(request, cancellationToken);
                    }
                }

                return response;
            }
            finally
            {
                throttler.Release();
            }

            static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
            {
                // Unix timestamp is seconds past epoch
                DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
                return dateTime;
            }
        }
    }
}
