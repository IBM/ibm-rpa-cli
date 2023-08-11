namespace Joba.IBM.RPA
{
    static class JsonSerializerOptionsFactory
    {
        internal static readonly JsonSerializerOptions SerializerOptions = CreateDefault();

        internal static JsonSerializerOptions CreateDefault(DirectoryInfo? workingDirectory = null)
        {
            var @default = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = new JsonKebabCaseNamingPolicy(),
                TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver(workingDirectory),
            };
            @default.Converters.Add(new DeploymentOptionJsonConverter());
            @default.Converters.Add(new AuthenticationMethodJsonConverter());
            @default.Converters.Add(new WalVersionJsonConverter());
            @default.Converters.Add(new NamePatternJsonConverter());
            @default.Converters.Add(new ParameterLocalRepositoryJsonConverterFactory());
            @default.Converters.Add(new RobotsJsonConverterFactory());
            @default.Converters.Add(new PropertyOptionsJsonConverter());
            @default.Converters.Add(new UniqueIdJsonConverter());
            return @default;
        }

        internal static JsonSerializerOptions CreateForProject(DirectoryInfo workingDirectory)
        {
            var options = CreateDefault(workingDirectory);
            options.Converters.Add(new PackageReferencesJsonConverterFactory(workingDirectory));
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
