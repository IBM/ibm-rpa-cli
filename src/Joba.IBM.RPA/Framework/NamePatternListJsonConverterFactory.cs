namespace Joba.IBM.RPA
{
    class NamePatternListJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeof(INames).IsAssignableFrom(typeToConvert);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return new NamePatternListJsonConverter();
        }

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
}