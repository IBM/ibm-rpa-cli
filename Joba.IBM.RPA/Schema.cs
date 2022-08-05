using System.Text.Json;

namespace Joba.IBM.RPA
{
    internal record struct Session(string Token, int TenantCode, Guid TenantId, string TenantName, string PersonName);

    internal record struct Profile(string RegionName, Uri RegionUrl, string Token, int TenantCode, Guid TenantId, string TenantName, string PersonName, string UserName, string Password)
    {
        public static string FilePath => Path.Combine(Constants.LocalFolder, $"{Environment.UserName}.json");

        public async Task SaveAsync(CancellationToken cancellation)
        {
            using var stream = File.OpenWrite(FilePath);
            await JsonSerializer.SerializeAsync(stream, this, Constants.SerializerOptions, cancellation);
        }

        public static Profile Create(Region region, Account account, Session session)
        {
            return new Profile(region.Name, region.ApiUrl, session.Token, session.TenantCode, session.TenantId, session.TenantName, session.PersonName, account.UserName, account.Password);
        }
    }
}
