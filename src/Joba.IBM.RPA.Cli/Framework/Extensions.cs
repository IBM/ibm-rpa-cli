using System.Net.Http.Headers;

namespace Joba.IBM.RPA.Cli
{
    internal static class Extensions
    {
        public static IRpaClient CreateClient(this Region region) =>
            new RpaClient(HttpRpaFactory.Create(region.ApiUrl));

        public static IRpaClient CreateClient(this Project project)
        {
            var client = HttpRpaFactory.Create(project.Session.Region);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", project.Session.Token);

            return new RpaClient(client);
        }
    }
}
