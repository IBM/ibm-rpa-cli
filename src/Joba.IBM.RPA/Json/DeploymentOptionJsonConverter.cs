namespace Joba.IBM.RPA
{
    public class DeploymentOptionJsonConverter : JsonConverter<DeploymentOption>
    {
        public override DeploymentOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string method;
            if (reader.TokenType == JsonTokenType.String)
                method = reader.GetString() ?? throw new JsonException();
            else
                throw new JsonException();

            return new DeploymentOption(method);
        }

        public override void Write(Utf8JsonWriter writer, DeploymentOption value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}