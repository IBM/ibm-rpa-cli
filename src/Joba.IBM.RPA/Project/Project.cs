using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public class Project
    {
        private readonly ILogger logger;
        private readonly ProjectFile projectFile;
        private readonly ProjectSettings projectSettings;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
        private readonly PackageSourcesFile packageSourcesFile;
        private readonly ILocalRepository repository;
        private readonly Environments environments;
        private PackageSources? packageSources;

        internal Project(ILogger logger, ProjectFile projectFile, ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings,
            PackageSourcesFile packageSourcesFile)
            : this(logger, projectFile, projectSettings, userFile, userSettings, packageSourcesFile, null) { }

        internal Project(ILogger logger, ProjectFile projectFile, ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings,
        PackageSourcesFile packageSourcesFile, PackageSources? packageSources)
        {
            this.logger = logger;
            this.projectFile = projectFile;
            this.projectSettings = projectSettings;
            this.userFile = userFile;
            this.userSettings = userSettings;
            this.packageSourcesFile = packageSourcesFile;
            this.packageSources = packageSources;
            repository = new LocalWalRepository(projectFile.WorkingDirectory);
            environments = new Environments(projectSettings, userFile, userSettings);
        }

        public DirectoryInfo RpaDirectory => projectFile.RpaDirectory;
        public DirectoryInfo WorkingDirectory => projectFile.WorkingDirectory;
        public string Name => projectFile.ProjectName;
        public IPackageSources PackageSources => packageSources ??= new PackageSources(projectSettings, userFile, userSettings);
        public IPackages Packages => projectSettings.Packages;
        public IRobots Robots => projectSettings.Robots;
        public ILocalRepository Files => repository;
        public IEnvironments Environments => environments;
        public ILocalRepository<Parameter> Parameters => projectSettings.Parameters;
        public IEnumerable<Uri> GetConfiguredRemoteAddresses() => projectSettings.Environments.Values.Select(v => v.Address).Distinct();

        public void EnsureCanConfigure(string alias)
        {
            if (projectSettings.EnvironmentExists(alias) || (packageSources?.SourceExists(alias)).GetValueOrDefault())
                throw new ProjectException($"Cannot configure '{alias}' because it's already being used. Aliases need to be unique among environments and package sources.");
        }

        public async Task<Environment> ConfigureEnvironment(IAccountResource resource, string alias,
            Region region, AccountCredentials credentials, CancellationToken cancellation)
        {
            var session = await credentials.AuthenticateAsync(resource, cancellation);
            var environment = EnvironmentFactory.Create(userFile, userSettings, alias, region, session);
            projectSettings.MapEnvironment(alias, environment.Remote);

            return environment;
        }

        public async Task SaveAsync(CancellationToken cancellation)
        {
            await projectFile.SaveAsync(projectSettings, cancellation);
            await userFile.SaveAsync(userSettings, cancellation);
            if (packageSources != null)
                await packageSourcesFile.SaveAsync(packageSources, cancellation);
        }

        public async Task<BuildResult> BuildAsync(WalFileName fileName, DirectoryInfo outputDirectory, CancellationToken cancellation)
        {
            var wal = repository.Get(fileName);
            if (wal == null)
                throw new ProjectException($"Could not find the file named '{fileName}'");

            var builder = new WalBuilder(logger, this);
            return await builder.BuildAsync(wal, outputDirectory, cancellation);
        }
    }
}