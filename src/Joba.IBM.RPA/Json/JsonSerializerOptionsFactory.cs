namespace Joba.IBM.RPA
{
    static class JsonSerializerOptionsFactory
    {
        internal static readonly JsonSerializerOptions SerializerOptions = CreateDefault();

        internal static JsonSerializerOptions CreateDefault()
        {
            var @default = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = new JsonKebabCaseNamingPolicy(),
                TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver(),
            };
            @default.Converters.Add(new WalVersionJsonConverter());
            @default.Converters.Add(new NamePatternJsonConverter());
            @default.Converters.Add(new NamePatternListJsonConverterFactory());
            @default.Converters.Add(new ParameterLocalRepositoryJsonConverterFactory());
            @default.Converters.Add(new PackageReferencesJsonConverterFactory());
            return @default;
        }

        internal static JsonSerializerOptions CreateForEnvironmentDependencies(DirectoryInfo environmentDirectory)
        {
            var options = CreateDefault();
            options.TypeInfoResolver = new EnvironmentDependenciesJsonTypeInfoResolver(environmentDirectory);
            return options;
        }

        internal static JsonSerializerOptions CreateForPackageSources(ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings)
        {
            var options = CreateDefault();
            options.Converters.Add(new PackageSourcesJsonConverterFactory(projectSettings, userFile, userSettings));
            return options;
        }

        internal static JsonSerializerOptions CreateForPackageSources()
        {
            var options = CreateDefault();
            options.Converters.Add(new PackageSourcesJsonConverterFactory());
            return options;
        }
    }
}
