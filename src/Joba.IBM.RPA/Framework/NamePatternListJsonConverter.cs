namespace Joba.IBM.RPA
{
    class NamePatternListJsonConverter : JsonConverter<INames>
    {
        public override INames? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            var values = new List<NamePattern>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                values.Add(JsonSerializer.Deserialize<NamePattern>(ref reader, options));

            return new NamePatternList(values);
        }

        public override void Write(Utf8JsonWriter writer, INames value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.AsEnumerable(), options);
        }
    }
}