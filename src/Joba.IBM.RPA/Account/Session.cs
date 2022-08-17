namespace Joba.IBM.RPA
{
    public struct Session
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; }        
        [JsonPropertyName("email")]
        public string UserName { get; init; }
        public int TenantCode { get; init; }
        public string TenantName { get; init; }
        [JsonPropertyName("name")]
        public string PersonName { get; init; }
    }
}
