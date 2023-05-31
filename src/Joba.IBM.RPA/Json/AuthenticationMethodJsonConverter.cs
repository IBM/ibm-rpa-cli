namespace Joba.IBM.RPA
{
    public class AuthenticationMethodJsonConverter : JsonConverter<AuthenticationMethod>
    {
        public override AuthenticationMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string method;
            if (reader.TokenType == JsonTokenType.String)
                method = reader.GetString() ?? throw new JsonException();
            else
                throw new JsonException();

            return new AuthenticationMethod(method);
        }

        public override void Write(Utf8JsonWriter writer, AuthenticationMethod value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}