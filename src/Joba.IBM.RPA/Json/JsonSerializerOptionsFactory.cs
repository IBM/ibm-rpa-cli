using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Joba.IBM.RPA
{
    static class JsonSerializerOptionsFactory
    {
        internal static readonly JsonSerializerOptions SerializerOptions = CreateDefault();

        internal static JsonSerializerOptions CreateDefault(DirectoryInfo? workingDirectory = null)
        {
            //https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding
            var encoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin);
            encoderSettings.AllowCharacter('\u0027'); //TODO: the character aphostofre (') should be allowed - this is not working

            var @default = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = new JsonKebabCaseNamingPolicy(),
                TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver(workingDirectory),
                Encoder = JavaScriptEncoder.Create(encoderSettings)
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
