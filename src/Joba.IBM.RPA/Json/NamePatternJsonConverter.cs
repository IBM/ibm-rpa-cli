namespace Joba.IBM.RPA
{
    class NamePatternJsonConverter : JsonConverter<NamePattern>
    {
        public override NamePattern Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var pattern = reader.GetString();
            if (pattern == null)
                throw new JsonException($"Could not create an instance of '{nameof(NamePattern)}'. The next token should be a '{nameof(String)}', but it's 'null'.");
            return new NamePattern(pattern);
        }

        public override void Write(Utf8JsonWriter writer, NamePattern value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}