namespace Joba.IBM.RPA
{
    public class PackageManager
    {
        private readonly IProject project;
        private readonly IPackageSourceResource source;

        internal PackageManager(IProject project, IPackageSourceResource source)
        {
            this.project = project;
            this.source = source;
        }

        public async Task<IEnumerable<PackageMetadata>> InstallAsync(NamePattern pattern, CancellationToken cancellation)
        {
            var packages = await source.SearchAsync(pattern, cancellation);
            if (!packages.Any())
                throw new PackageNotFoundException(pattern.ToString(), $"No packages were found with the query '{pattern}'.");

            var metadata = packages.Select(p => p.Metadata).ToArray();
            foreach (var package in packages)
                _ = project.Packages.Install(package);

            await project.SaveAsync(cancellation);
            return metadata;
        }

        public async Task<PackageMetadata> InstallAsync(string name, WalVersion version, CancellationToken cancellation)
        {
            var current = project.Packages.Get(name);
            if (current != null)
                throw new PackageAlreadyInstalledException(name, version);

            var package = await source.GetAsync(name, version, cancellation);
            if (package == null)
                throw new PackageNotFoundException(name, version);

            _ = project.Packages.Install(package);
            await project.SaveAsync(cancellation);
            return package.Metadata;
        }

        public async Task<UpdatePackageOperation> UpdateAsync(string name, WalVersion? version, CancellationToken cancellation)
        {
            var metadata = project.Packages.Get(name);
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
                var operation = project.Packages.Update(package);
                await project.SaveAsync(cancellation);

                return operation;
            }

            return new UpdatePackageOperation(metadata, package.Metadata);
        }

        public async Task<UpdateAllPackagesOperation> UpdateAllAsync(CancellationToken cancellation)
        {
            var allOperation = new UpdateAllPackagesOperation();
            var packages = project.Packages.ToList();
            foreach (var metadata in packages)
            {
                var package = await source.GetLatestAsync(metadata.Name, cancellation);
                if (package == null)
                    throw new PackageNotFoundException(metadata.Name);

                var operation = project.Packages.Update(package);
                allOperation.Add(operation);
            }

            await project.SaveAsync(cancellation);
            return allOperation;
        }

        public async Task<IEnumerable<PackageMetadata>> RestoreAsync(CancellationToken cancellation)
        {
            var packages = project.Packages.ToList();
            foreach (var metadata in packages)
            {
                var package = await source.GetAsync(metadata.Name, metadata.Version, cancellation);
                if (package == null)
                    throw new PackageNotFoundException(metadata.Name, metadata.Version);

                _ = project.Packages.Restore(package);
            }

            return packages;
        }

        public async Task<IEnumerable<PackageMetadata>> UninstallAsync(NamePattern pattern, CancellationToken cancellation)
        {
            var packages = project.Packages.Where(p => pattern.Matches(p.Name)).ToList();
            if (packages.Any())
            {
                foreach (var package in packages)
                    project.Packages.Uninstall(package);

                await project.SaveAsync(cancellation);
            }

            return packages;
        }

        public async Task<IEnumerable<PackageMetadata>> UninstallAllAsync(CancellationToken cancellation)
        {
            var packages = project.Packages.ToList();
            project.Packages.UninstallAll();
            await project.SaveAsync(cancellation);
            return packages;
        }
    }

    public record class UpdatePackageOperation(PackageMetadata Previous, PackageMetadata New)
    {
        public bool HasBeenUpdated => Previous.Version != New.Version;
    }

    public class UpdateAllPackagesOperation
    {
        private readonly IList<UpdatePackageOperation> operations = new List<UpdatePackageOperation>();

        public bool HasBeenUpdated => operations.Any(o => o.HasBeenUpdated);
        public IEnumerable<UpdatePackageOperation> Operations => operations;
        internal void Add(UpdatePackageOperation operation) => operations.Add(operation);
    }
}