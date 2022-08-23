namespace Joba.IBM.RPA
{
    class PackageReferencesJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeof(ILocalRepository<PackageMetadata>).IsAssignableFrom(typeToConvert);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            new PackageReferencesJsonConverter();

        class PackageReferencesJsonConverter : JsonConverter<ILocalRepository<PackageMetadata>>
        {
            public override ILocalRepository<PackageMetadata>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();

                var values = JsonSerializer.Deserialize<Dictionary<string, WalVersion>>(ref reader, options) ??
                    throw new JsonException();

                return new PackageReferences(values);
            }

            public override void Write(Utf8JsonWriter writer, ILocalRepository<PackageMetadata> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.ToDictionary(k => k.Name, v => v.Version), options);
            }
        }
    }
}