namespace Joba.IBM.RPA
{
    public static class Extensions
    {
        public static PackageMetadata ToPackage(this WalFile wal) => 
            new (wal.Name, wal.Version ?? throw new InvalidOperationException($"Cannot convert '{wal.Name}' to package because it does not have a version."));

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

        public static async Task ThrowWhenUnsuccessful(this HttpResponseMessage response, CancellationToken cancellation = default)
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

        public static string Trace(this Exception exception, string? label = null)
        {
            var arg = string.IsNullOrWhiteSpace(label) ? string.Empty : label + ": ";
            var text = $"{arg}{exception}, HResult {exception.HResult}";
            System.Diagnostics.Trace.TraceError(text);
            return text;
        }

        public static void CreateHidden(this DirectoryInfo dir)
        {
            dir.Create();
            dir.Attributes |= FileAttributes.Hidden;
        }
    }
}
