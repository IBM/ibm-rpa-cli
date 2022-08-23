namespace Joba.IBM.RPA
{
    public class WalVersionJsonConverter : JsonConverter<WalVersion>
    {
        public override WalVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int version;
            if (reader.TokenType == JsonTokenType.String)
                version = int.Parse(reader.GetString() ?? throw new JsonException());
            else if (reader.TokenType == JsonTokenType.Number)
                version = reader.GetInt32();
            else
                throw new JsonException();

            return new WalVersion(version);
        }

        public override void Write(Utf8JsonWriter writer, WalVersion value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}