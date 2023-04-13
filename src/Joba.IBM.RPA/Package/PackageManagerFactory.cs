namespace Joba.IBM.RPA
{
    public interface IPackageManagerFactory
    {
        IPackageManager Create(IProject project, string? sourceAlias = null);
    }
    
    public sealed class PackageManagerFactory : IPackageManagerFactory
    {
        private readonly IRpaClientFactory clientFactory;

        public PackageManagerFactory(IRpaClientFactory clientFactory) => this.clientFactory = clientFactory;

        IPackageManager IPackageManagerFactory.Create(IProject project, string? sourceAlias = null)
        {
            if (sourceAlias == null)
                return new PackageManager(project, new MultiplePackageSourceResource(CreateSources(project).ToArray()));
            return new PackageManager(project, CreateSource(project, sourceAlias));
        }

        private IEnumerable<IPackageSourceResource> CreateSources(IProject project)
        {
            foreach (var source in project.PackageSources)
                yield return CreateSource(source);
        }

        private IPackageSourceResource CreateSource(IProject project, string sourceAlias)
        {
            var source = project.PackageSources.Get(sourceAlias);
            if (source == null)
                throw new PackageSourceNotFoundException(sourceAlias);

            return CreateSource(source.Value);
        }

        private IPackageSourceResource CreateSource(PackageSource source)
        {
            var client = clientFactory.CreateFromPackageSource(source);
            return new PackageSourceResource(client, source);
        }
    }

    internal class PackageSourceResource : IPackageSourceResource
    {
        private readonly IRpaClient client;
        private readonly PackageSource packageSource;

        internal PackageSourceResource(IRpaClient client, PackageSource packageSource)
        {
            this.client = client;
            this.packageSource = packageSource;
        }

        async Task<IEnumerable<Package>> IPackageSourceResource.SearchAsync(NamePattern pattern, CancellationToken cancellation)
        {
            var scripts = (await client.Script.SearchAsync(pattern.Name, 50, cancellation)).Where(s => pattern.Matches(s.Name)).ToArray();
            var tasks = scripts
                .Select(s => client.Script.GetLatestVersionAsync(s.Name, cancellation)
                    .ContinueWith(c => c.Result ?? throw new PackageNotFoundException(s.Name, $"Could not find latest version of '{s.Name}'"), TaskContinuationOptions.OnlyOnRanToCompletion)
                    .ContinueWith(c => Package.From(c.Result), TaskContinuationOptions.OnlyOnRanToCompletion))
                .ToList();

            return await Task.WhenAll(tasks);

        }

        async Task<Package?> IPackageSourceResource.GetAsync(string name, WalVersion version, CancellationToken cancellation)
        {
            var script = await client.Script.GetAsync(name, version, cancellation);
            if (script == null)
                return null;

            return Package.From(script);
        }

        async Task<Package?> IPackageSourceResource.GetLatestAsync(string name, CancellationToken cancellation)
        {
            var script = await client.Script.GetLatestVersionAsync(name, cancellation);
            if (script == null)
                return null;

            return Package.From(script);
        }
    }

    internal class MultiplePackageSourceResource : IPackageSourceResource
    {
        private readonly IPackageSourceResource[] sources;

        public MultiplePackageSourceResource(IPackageSourceResource[] sources)
        {
            if (sources.Length == 0)
                throw new PackageSourceException("No package source is configured.");
            this.sources = sources;
        }

        async Task<IEnumerable<Package>> IPackageSourceResource.SearchAsync(NamePattern pattern, CancellationToken cancellation)
        {
            foreach (var source in sources)
            {
                var packages = await source.SearchAsync(pattern, cancellation);
                if (packages.Any())
                    return packages;
            }

            return Enumerable.Empty<Package>();
        }

        async Task<Package?> IPackageSourceResource.GetAsync(string name, WalVersion version, CancellationToken cancellation)
        {
            foreach (var source in sources)
            {
                var package = await source.GetAsync(name, version, cancellation);
                if (package != null)
                    return package;
            }

            return null;
        }

        async Task<Package?> IPackageSourceResource.GetLatestAsync(string name, CancellationToken cancellation)
        {
            foreach (var source in sources)
            {
                var package = await source.GetLatestAsync(name, cancellation);
                if (package != null)
                    return package;
            }

            return null;
        }
    }

    internal interface IPackageSourceResource
    {
        Task<IEnumerable<Package>> SearchAsync(NamePattern pattern, CancellationToken cancellation);
        Task<Package?> GetAsync(string name, WalVersion version, CancellationToken cancellation);
        Task<Package?> GetLatestAsync(string name, CancellationToken cancellation);
    }
}