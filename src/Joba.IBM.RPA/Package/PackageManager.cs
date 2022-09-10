namespace Joba.IBM.RPA
{
    public class PackageManager
    {
        private readonly Project project;
        private readonly Environment environment;
        private readonly IPackageSourceResource source;

        internal PackageManager(Project project, Environment environment, IPackageSourceResource source)
        {
            this.project = project;
            this.environment = environment;
            this.source = source;
        }

        public async Task<IEnumerable<PackageMetadata>> InstallAsync(NamePattern pattern, CancellationToken cancellation)
        {
            var packages = await source.SearchAsync(pattern, cancellation);
            if (!packages.Any())
                throw new PackageNotFoundException(pattern.ToString(), $"No packages were found with the query '{pattern}'.");

            var metadata = packages.Select(p => p.Metadata).ToArray();
            project.Dependencies.Packages.AddOrUpdate(metadata.OrderBy(p => p.Name).ToArray());
            foreach (var package in packages)
                environment.Dependencies.Packages.Create(package.Script);

            await environment.SaveAsync(cancellation);
            await project.SaveAsync(cancellation);
            return metadata;
        }

        public async Task<PackageMetadata> InstallAsync(string name, WalVersion version, CancellationToken cancellation)
        {
            var current = project.Dependencies.Packages.Get(name);
            if (current != null)
                throw new PackageAlreadyInstalledException(name, version);

            var package = await source.GetAsync(name, version, cancellation);
            if (package == null)
                throw new PackageNotFoundException(name, version);

            project.Dependencies.Packages.AddOrUpdate(package.Metadata);
            environment.Dependencies.Packages.Create(package.Script);

            await environment.SaveAsync(cancellation);
            await project.SaveAsync(cancellation);
            return package.Metadata;
        }

        public async Task<UpdatePackageOperation> UpdateAsync(string name, WalVersion? version, CancellationToken cancellation)
        {
            var metadata = project.Dependencies.Packages.Get(name);
            if (metadata == null)
                throw new PackageNotFoundException(name);
            if (metadata.Version == version)
                throw new PackageException(name, $"The package '{name}' is already on version '{version}'");

            var getTask = version != null ?
                source.GetAsync(name, version.Value, cancellation) :
                source.GetLatestAsync(name, cancellation);

            var package = await getTask;
            if (package == null)
                if (version != null)
                    throw new PackageNotFoundException(name, version.Value);
                else
                    throw new PackageNotFoundException(name);

            if (metadata.Version != package.Metadata.Version)
            {
                project.Dependencies.Packages.Update(package.Metadata);
                environment.Dependencies.Packages.Create(package.Script);

                var updater = new PackageReferenceUpdater(environment);
                var affected = updater.UpdateTo(package.Metadata).ToArray();

                await environment.SaveAsync(cancellation);
                await project.SaveAsync(cancellation);

                return new UpdatePackageOperation(metadata, package.Metadata, affected);
            }

            return new UpdatePackageOperation(metadata, package.Metadata, Enumerable.Empty<WalFile>().ToArray());
        }

        public async Task<UpdateAllPackagesOperation> UpdateAllAsync(CancellationToken cancellation)
        {
            var updater = new PackageReferenceUpdater(environment);
            var operation = new UpdateAllPackagesOperation();
            var packages = project.Dependencies.Packages.ToList();
            foreach (var metadata in packages)
            {
                var package = await source.GetLatestAsync(metadata.Name, cancellation);
                if (package == null)
                    throw new PackageNotFoundException(metadata.Name);

                project.Dependencies.Packages.Update(package.Metadata);
                environment.Dependencies.Packages.Create(package.Script);

                var affected = updater.UpdateTo(package.Metadata).ToArray();
                operation.Add(metadata, package.Metadata, affected);
            }

            await project.SaveAsync(cancellation);
            await environment.SaveAsync(cancellation);

            return operation;
        }

        public async Task<IEnumerable<PackageMetadata>> RestoreAsync(CancellationToken cancellation)
        {
            var packages = project.Dependencies.Packages.ToList();
            foreach (var metadata in packages)
            {
                var package = await source.GetAsync(metadata.Name, metadata.Version, cancellation);
                if (package == null)
                    throw new PackageNotFoundException(metadata.Name, metadata.Version);

                environment.Dependencies.Packages.Create(package.Script);
            }

            await environment.SaveAsync(cancellation);
            return packages;
        }

        public async Task<IEnumerable<PackageMetadata>> UninstallAsync(NamePattern pattern, CancellationToken cancellation)
        {
            var packages = project.Dependencies.Packages.Where(p => pattern.Matches(p.Name)).ToList();
            if (packages.Any())
            {
                foreach (var package in packages)
                {
                    environment.Dependencies.Packages.Delete(package.Name);
                    project.Dependencies.Packages.Remove(package.Name);
                }

                await environment.SaveAsync(cancellation);
                await project.SaveAsync(cancellation);
            }

            return packages;
        }

        public async Task<IEnumerable<PackageMetadata>> UninstallAllAsync(CancellationToken cancellation)
        {
            var packages = project.Dependencies.Packages.ToList();
            if (packages.Any())
            {
                foreach (var package in packages)
                    environment.Dependencies.Packages.Delete(package.Name);

                project.Dependencies.Packages.Clear();
                await environment.SaveAsync(cancellation);
                await project.SaveAsync(cancellation);
            }

            return packages;
        }
    }

    public record class UpdatePackageOperation(PackageMetadata Old, PackageMetadata New, WalFile[] Files)
    {
        public bool HasBeenUpdated => Old.Version != New.Version;
    }

    public class UpdateAllPackagesOperation
    {
        private readonly IList<UpdatePackageOperation> operations = new List<UpdatePackageOperation>();

        public bool HasBeenUpdated => operations.Any(o => o.HasBeenUpdated);
        public IEnumerable<UpdatePackageOperation> Operations => operations;
        public IEnumerable<WalFile> Files => operations.SelectMany(o => o.Files).DistinctBy(f => f.Name);

        internal void Add(PackageMetadata old, PackageMetadata @new, WalFile[] files)
        {
            operations.Add(new UpdatePackageOperation(old, @new, files));
        }
    }

    class PackageReferenceUpdater
    {
        private readonly Environment environment;

        public PackageReferenceUpdater(Environment environment)
        {
            this.environment = environment;
        }

        public IEnumerable<WalFile> UpdateTo(PackageMetadata package)
        {
            var affected = new List<WalFile>();
            foreach (var wal in environment.Files)
            {
                var parser = new WalParser(wal.Content);
                var lines = parser.Parse();
                var analyzer = new WalAnalyzer(lines);
                var references = analyzer.FindPackages(package.Name);
                if (references.Any())
                {
                    references.Replace(package.Version);
                    wal.Overwrite(lines.Build());
                    affected.Add(wal);
                }
            }

            return affected;
        }
    }
}