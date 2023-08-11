namespace Joba.IBM.RPA
{
    /// <summary>
    /// Must only contain latin letters (a-z, A-Z), numbers (0-9), underscores ( _ ) and cannot start with a number.
    /// </summary>
    public struct UniqueId
    {
        private readonly string uniqueId;

        public UniqueId(string name)
        {
            Original = name;
            //TODO: use regex to replace everything except the allowed values.
            uniqueId = name.Replace(" ", "_").Replace("-", "_");
        }

        public string Original { get; }

        public override string ToString() => uniqueId;

        public static implicit operator string(UniqueId id) => id.uniqueId;
    }

    public class UniqueIdJsonConverter : JsonConverter<UniqueId>
    {
        public override UniqueId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string uniqueId;
            if (reader.TokenType == JsonTokenType.String)
                uniqueId = reader.GetString() ?? throw new JsonException();
            else
                throw new JsonException();

            return new UniqueId(uniqueId);
        }

        public override void Write(Utf8JsonWriter writer, UniqueId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
