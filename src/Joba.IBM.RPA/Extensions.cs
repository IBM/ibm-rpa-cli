namespace Joba.IBM.RPA
{
    public static class Extensions
    {
        internal static (List<NamePattern>, List<string>) Split(this IEnumerable<NamePattern> collection)
        {
            var withWildcards = new List<NamePattern>();
            var withoutWildcards = new List<string>();
            foreach (var parameter in collection)
            {
                if (parameter.HasWildcard)
                    withWildcards.Add(parameter);
                else
                    withoutWildcards.Add(parameter.Name);
            }

            return (withWildcards, withoutWildcards);
        }

        public static async Task ThrowWhenUnsuccessfulAsync(this HttpResponseMessage response, CancellationToken cancellation = default)
        {
            if (response.IsSuccessStatusCode)
                return;

            var reason = response.ReasonPhrase;
            var content = default(string);
            if (response.Content != null)
                content = await response.Content.ReadAsStringAsync(cancellation);

            var builder = new StringBuilder($"The request failed with status {response.StatusCode}");
            if (!string.IsNullOrEmpty(reason))
                builder.AppendFormat(", reason {0}", reason);
            if (!string.IsNullOrEmpty(content))
            {
                builder.Append(". Details:");
                builder.AppendLine(content);
            }

            throw new HttpRequestException(builder.ToString(), null, response.StatusCode);
        }
    }
}
