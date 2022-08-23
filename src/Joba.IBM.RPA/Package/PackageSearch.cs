namespace Joba.IBM.RPA
{
    public class PackageSearch
    {
        private readonly IRpaClient client;

        public PackageSearch(IRpaClient client)
        {
            this.client = client;
        }

        public async Task<IEnumerable<PackageMetadata>> SearchAsync(NamePattern pattern, CancellationToken cancellation)
        {
            var scripts = (await client.Script.SearchAsync(pattern.Name, 50, cancellation)).Where(s => pattern.Matches(s.Name)).ToArray();
            var tasks = scripts
                .Select(s => client.Script.GetLatestVersionAsync(s.Name, cancellation)
                    .ContinueWith(c => c.Result ?? throw new Exception($"Could not find latest version of '{s.Name}'"), TaskContinuationOptions.OnlyOnRanToCompletion)
                    .ContinueWith(c => new PackageMetadata(s.Name, c.Result.Version), TaskContinuationOptions.OnlyOnRanToCompletion))
                .ToList();

            return await Task.WhenAll(tasks);
        }
    }
}
