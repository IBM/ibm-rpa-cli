namespace Joba.IBM.RPA
{
    class RobotsJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => typeof(IRobots).IsAssignableFrom(typeToConvert);

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            new RobotsJsonConverter();

        class RobotsJsonConverter : JsonConverter<IRobots>
        {
            public override IRobots? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();

                var values = JsonSerializer.Deserialize<Dictionary<string, RobotSettings>>(ref reader, options) ??
                    throw new JsonException();

                return new Robots(values);
            }

            public override void Write(Utf8JsonWriter writer, IRobots value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.ToDictionary(k => k.Name, v => v.Settings), options);
            }
        }
    }
}