namespace Joba.IBM.RPA
{
    class ParameterDependenciesJsonConverter : JsonConverter<IParameterDependencies>
    {
        public override IParameterDependencies? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            var values = new List<NamePattern>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                values.Add(JsonSerializer.Deserialize<NamePattern>(ref reader, options));

            return new ProjectSettings.ParameterDependencies(values);
        }

        public override void Write(Utf8JsonWriter writer, IParameterDependencies value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.AsEnumerable(), options);
        }
    }
}