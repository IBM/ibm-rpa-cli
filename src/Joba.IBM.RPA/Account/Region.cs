namespace Joba.IBM.RPA
{
    public record class Region(string Name, [property: JsonPropertyName("ApiUrl")] Uri ApiAddress, [property: JsonPropertyName("WebUrl")] Uri WebAddress, string? Description = null)
    {
        public override string ToString() => ApiAddress.ToString();
    }
}
