namespace Joba.IBM.RPA
{
    class PackageSourcesJsonConverterFactory : JsonConverterFactory
    {
        private readonly ProjectSettings? projectSettings;
        private readonly UserSettingsFile? userFile;
        private readonly UserSettings? userSettings;

        public PackageSourcesJsonConverterFactory() { }

        public PackageSourcesJsonConverterFactory(ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings)
        {
            this.projectSettings = projectSettings;
            this.userFile = userFile;
            this.userSettings = userSettings;
        }

        public override bool CanConvert(Type typeToConvert) => typeof(IPackageSources).IsAssignableFrom(typeToConvert);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            projectSettings == null || userFile == null || userSettings == null
                ? new WriteJsonConverter()
                : new ReadJsonConverter(projectSettings, userFile.Value, userSettings);

        class ReadJsonConverter : JsonConverter<IPackageSources>
        {
            private readonly ProjectSettings projectSettings;
            private readonly UserSettingsFile userFile;
            private readonly UserSettings userSettings;

            public ReadJsonConverter(ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings)
            {
                this.projectSettings = projectSettings;
                this.userFile = userFile;
                this.userSettings = userSettings;
            }

            public override IPackageSources? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();

                var values = JsonSerializer.Deserialize<Dictionary<string, RemoteSettings>>(ref reader, options) ??
                    throw new JsonException();

                return new PackageSources(projectSettings, userFile, userSettings, values);
            }

            public override void Write(Utf8JsonWriter writer, IPackageSources value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        class WriteJsonConverter : JsonConverter<IPackageSources>
        {
            public override IPackageSources? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, IPackageSources value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.ToDictionary(k => k.Alias, v => v.Remote), options);
            }
        }
    }
}