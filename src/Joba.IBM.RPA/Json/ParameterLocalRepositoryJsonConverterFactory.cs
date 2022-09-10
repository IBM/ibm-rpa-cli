namespace Joba.IBM.RPA
{
    class ParameterLocalRepositoryJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeof(ILocalRepository<Parameter>).IsAssignableFrom(typeToConvert);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            new ParameterLocalRepositoryJsonConverter();

        class ParameterLocalRepositoryJsonConverter : JsonConverter<ILocalRepository<Parameter>>
        {
            public override ILocalRepository<Parameter>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();

                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options) ??
                    throw new JsonException();

                return new ParameterRepository(values);
            }

            public override void Write(Utf8JsonWriter writer, ILocalRepository<Parameter> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.ToDictionary(k => k.Name, v => v.Value), options);
            }
        }
    }
}