using System.Text.Json;

namespace Joba.IBM.RPA
{
    internal record class Profile(string RegionName, Uri RegionUrl, string Token, int TenantCode, Guid TenantId, string TenantName, string PersonName, string UserName, string Password)
    {
        public static string FilePath => Path.Combine(Constants.LocalFolder, $"{Environment.UserName}.json");

        public async Task SaveAsync(CancellationToken cancellation)
        {
            using var stream = File.OpenWrite(FilePath);
            await JsonSerializer.SerializeAsync(stream, this, Constants.SerializerOptions, cancellation);
        }

        public RpaClient CreateClient()
        {
            return new RpaClient(HttpFactory.Create(RegionUrl)); //TODO: create handler to authenticate again if token is expired
        }

        public static async Task<Profile> LoadAsync(CancellationToken cancellation)
        {
            if (!File.Exists(FilePath))
                throw new Exception($"Could not find '{Environment.UserName}' profile. Please use '{Constants.CliName} {ConfigureCommand.CommandName}' command to create one.");

            using var stream = File.OpenRead(FilePath);
            return await JsonSerializer.DeserializeAsync<Profile>(stream, Constants.SerializerOptions, cancellation);
        }

        public static Profile Create(Region region, Account account, Session session)
        {
            return new Profile(region.Name, region.ApiUrl, session.Token, session.TenantCode, session.TenantId, session.TenantName, session.PersonName, account.UserName, account.Password);
        }
    }
}
