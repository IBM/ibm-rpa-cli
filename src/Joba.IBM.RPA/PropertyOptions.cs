namespace Joba.IBM.RPA
{
    public class PropertyOptions : ICollection<KeyValuePair<string, string>>
    {
        public const string CloudPakConsoleAddress = "cpconsole";
        private readonly IDictionary<string, string> properties;

        public PropertyOptions() : this(new Dictionary<string, string>()) { }
        internal PropertyOptions(IDictionary<string, string> properties) => this.properties = properties;

        public string? this[string key] => properties.ContainsKey(key) ? properties[key] : null;
        public int Count => properties.Count;

        public static PropertyOptions Parse(IEnumerable<string> values)
        {
            var properties = new Dictionary<string, string>();
            foreach (var value in values)
            {
                var splitted = value.Split('=');
                if (splitted.Length == 0 || splitted.Length > 2)
                    throw new FormatException($"The property '{value}' is not in the correct format. It should be [key]=[value].");

                properties.Add(splitted[0], splitted[1]);
            }

            return new PropertyOptions(properties);
        }

        internal IDictionary<string, string> ToDictionary() => properties;
        public override string? ToString() => properties.ToString();

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly => properties.IsReadOnly;
        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => properties.Add(item);
        void ICollection<KeyValuePair<string, string>>.Clear() => properties.Clear();
        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) => properties.Contains(item);
        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => properties.CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) => properties.Remove(item);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => properties.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)properties).GetEnumerator();
    }
}
