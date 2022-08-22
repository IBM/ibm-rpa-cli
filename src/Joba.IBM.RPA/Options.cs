namespace Joba.IBM.RPA
{
    static class Options
    {
        public static readonly JsonSerializerOptions SerializerOptions;

        static Options()
        {
            SerializerOptions = new()
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver(),
            };
            SerializerOptions.Converters.Add(new NamePatternJsonConverter());
            SerializerOptions.Converters.Add(new NamePatternListJsonConverter());
        }
    }
}
