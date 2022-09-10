namespace Joba.IBM.RPA
{
    public record struct Session(string Token, DateTime ExpirationDate, int TenantCode, string TenantName, string UserName, string PersonName)
    {
        public static readonly Session NoSession = new();

        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpirationDate;

        internal static Session From(CreatedSession session) =>
            new(session.AccessToken, DateTime.UtcNow.AddSeconds(session.ExpiresIn),
                session.TenantCode, session.TenantName, session.UserName, session.PersonName);
    }

    public struct CreatedSession
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
        [JsonPropertyName("email")]
        public string UserName { get; init; }
        public int TenantCode { get; init; }
        public string TenantName { get; init; }
        [JsonPropertyName("name")]
        public string PersonName { get; init; }
    }
}
