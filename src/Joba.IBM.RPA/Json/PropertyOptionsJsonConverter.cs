namespace Joba.IBM.RPA
{
    public class PropertyOptionsJsonConverter : JsonConverter<PropertyOptions>
    {
        public override PropertyOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options) ??
                throw new JsonException();

            return new PropertyOptions(values);
        }

        public override void Write(Utf8JsonWriter writer, PropertyOptions value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.ToDictionary(), options);
        }
    }
}