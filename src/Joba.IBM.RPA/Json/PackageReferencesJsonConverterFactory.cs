namespace Joba.IBM.RPA
{
    class PackageReferencesJsonConverterFactory : JsonConverterFactory
    {
        private readonly DirectoryInfo workingDirectory;

        public PackageReferencesJsonConverterFactory(DirectoryInfo workingDirectory) => this.workingDirectory = workingDirectory;

        public override bool CanConvert(Type typeToConvert) => typeof(IPackages).IsAssignableFrom(typeToConvert);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            new PackageReferencesJsonConverter(workingDirectory);

        class PackageReferencesJsonConverter : JsonConverter<IPackages>
        {
            private readonly DirectoryInfo workingDirectory;

            public PackageReferencesJsonConverter(DirectoryInfo workingDirectory) => this.workingDirectory = workingDirectory;

            public override IPackages? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();

                var values = JsonSerializer.Deserialize<Dictionary<string, WalVersion>>(ref reader, options) ??
                    throw new JsonException();

                return new PackageReferences(workingDirectory, values);
            }

            public override void Write(Utf8JsonWriter writer, IPackages value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.ToDictionary(k => k.Name, v => v.Version), options);
            }
        }
    }
}