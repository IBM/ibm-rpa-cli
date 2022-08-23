namespace Joba.IBM.RPA
{
    public class PackageManager
    {
        private readonly Project project;
        private readonly Environment environment;
        private readonly IRpaClient client;

        public PackageManager(IRpaClient client, Project project, Environment environment)
        {
            this.client = client;
            this.project = project;
            this.environment = environment;
        }

        public async Task<IEnumerable<PackageMetadata>> InstallAsync(NamePattern pattern, CancellationToken cancellation)
        {
            var packageSearch = new PackageSearch(client);
            var packages = await packageSearch.SearchAsync(pattern, cancellation);
            if (!packages.Any())
                throw new Exception($"No packages were found with the query '{pattern}'.");

            project.Dependencies.Packages.AddOrUpdate(packages.OrderBy(p => p.Name).ToArray());
            foreach (var package in packages)
                await environment.Dependencies.Packages.DownloadAsync(client.Script, package.Name, package.Version, cancellation);

            await environment.SaveAsync(cancellation);
            await project.SaveAsync(cancellation);
            return packages;
        }

        public async Task<PackageMetadata> InstallAsync(string name, WalVersion version, CancellationToken cancellation)
        {
            var current = project.Dependencies.Packages.Get(name);
            if (current != null)
                throw new PackageAlreadyInstalledException(name, version);

            var script = await client.Script.GetAsync(name, version, cancellation);
            if (script == null)
                throw new Exception($"Could not find package '{name}' with version '{version}'.");

            var package = new PackageMetadata(script.Name, script.Version);
            project.Dependencies.Packages.AddOrUpdate(package);
            environment.Dependencies.Packages.Create(script);

            await environment.SaveAsync(cancellation);
            await project.SaveAsync(cancellation);
            return package;
        }

        public async Task<UpdatePackageOperation> UpdateAsync(string name, WalVersion? version, CancellationToken cancellation)
        {
            var package = project.Dependencies.Packages.Get(name);
            if (package == null)
                throw new PackageNotFoundException(name);
            if (package.Version == version)
                throw new Exception($"The package '{name}' is already on version '{version}'");

            var downloadTask = version != null ?
                environment.Dependencies.Packages.DownloadAsync(client.Script, name, version.Value, cancellation) :
                environment.Dependencies.Packages.DownloadLatestAsync(client.Script, name, cancellation);

            var wal = await downloadTask;
            var updatedPackage = wal.ToPackage();
            if (updatedPackage.Version != package.Version)
            {
                var updater = new PackageReferenceUpdater(project, environment);
                updater.UpdateTo(updatedPackage);

                await project.SaveAsync(cancellation);
                await environment.SaveAsync(cancellation);
            }

            return new UpdatePackageOperation(package, updatedPackage, Enumerable.Empty<WalFile>().ToArray());
        }

        public async Task<UpdateAllPackagesOperation> UpdateAllAsync(CancellationToken cancellation)
        {
            var updater = new PackageReferenceUpdater(project, environment);
            var operation = new UpdateAllPackagesOperation();
            var packages = project.Dependencies.Packages.ToList();
            foreach (var package in packages)
            {
                var wal = await environment.Dependencies.Packages.DownloadLatestAsync(client.Script, package.Name, cancellation);
                var updatedPackage = wal.ToPackage();
                updater.UpdateTo(updatedPackage);
                operation.Add(package, updatedPackage, Enumerable.Empty<WalFile>().ToArray());
            }

            await project.SaveAsync(cancellation);
            await environment.SaveAsync(cancellation);

            return operation;
        }

        public async Task<IEnumerable<PackageMetadata>> RestoreAsync(CancellationToken cancellation)
        {
            var packages = project.Dependencies.Packages.ToList();
            foreach (var package in packages)
                _ = await environment.Dependencies.Packages.DownloadAsync(client.Script, package.Name, package.Version, cancellation);

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

                await project.SaveAsync(cancellation);
                await environment.SaveAsync(cancellation);
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
                await project.SaveAsync(cancellation);
                await environment.SaveAsync(cancellation);
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
        private readonly Project project;
        private readonly Environment environment;

        public PackageReferenceUpdater(Project project, Environment environment)
        {
            this.project = project;
            this.environment = environment;
        }

        public void UpdateTo(PackageMetadata package)
        {
            project.Dependencies.Packages.Update(package);
            foreach (var wal in environment.Files)
            {
                var parser = new WalParser(wal.Content);
                var lines = parser.Parse();
                var analyzer = new WalAnalyzer(lines);
                var references = analyzer.FindPackages(package.Name);

                references.Replace(package.Version);
                wal.Overwrite(lines.Build());
            }
        }
    }
}