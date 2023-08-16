using Joba.IBM.RPA.Server;

namespace Joba.IBM.RPA
{
    internal class Project : IProject
    {
        private readonly ProjectFile projectFile;
        private readonly ProjectSettings projectSettings;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
        private readonly PackageSourcesFile packageSourcesFile;
        private readonly IScriptRepository repository;
        private readonly Environments environments;
        private PackageSources? packageSources;

        internal Project(ProjectFile projectFile, ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings,
            PackageSourcesFile packageSourcesFile)
            : this(projectFile, projectSettings, userFile, userSettings, packageSourcesFile, null) { }

        internal Project(ProjectFile projectFile, ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings,
        PackageSourcesFile packageSourcesFile, PackageSources? packageSources)
        {
            this.projectFile = projectFile;
            this.projectSettings = projectSettings;
            this.userFile = userFile;
            this.userSettings = userSettings;
            this.packageSourcesFile = packageSourcesFile;
            this.packageSources = packageSources;
            repository = new ScriptRepository(projectFile.WorkingDirectory);
            environments = new Environments(projectSettings, userFile, userSettings);
        }

        public DirectoryInfo RpaDirectory => projectFile.RpaDirectory;
        public DirectoryInfo WorkingDirectory => projectFile.WorkingDirectory;
        public string Name => projectFile.ProjectName;
        public string Description => projectSettings.Description;
        public IPackageSources PackageSources => packageSources ??= new PackageSources(projectSettings, userFile, userSettings);
        public IPackages Packages => projectSettings.Packages;
        public IRobots Robots => projectSettings.Robots;
        public IScriptRepository Scripts => repository;
        public IEnvironments Environments => environments;
        public ILocalRepository<Parameter> Parameters => projectSettings.Parameters;
        public IEnumerable<Uri> GetConfiguredRemoteAddresses() => projectSettings.Environments.Values.Select(v => v.Address).Distinct();

        public void EnsureCanConfigure(string alias)
        {
            if (projectSettings.EnvironmentExists(alias) || (packageSources?.SourceExists(alias)).GetValueOrDefault())
                throw new ProjectException($"Cannot configure '{alias}' because it's already being used. Aliases need to be unique among environments and package sources.");
        }

        public Environment ConfigureEnvironment(string alias, Region region, CreatedSession session, ServerConfig server, PropertyOptions properties)
        {
            var environment = EnvironmentFactory.Create(userFile, userSettings, alias, region, session, server, properties);
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
    }

    public interface IProject
    {
        DirectoryInfo RpaDirectory { get; }
        DirectoryInfo WorkingDirectory { get; }
        string Name { get; }
        string Description { get; }
        IPackageSources PackageSources { get; }
        IPackages Packages { get; }
        IRobots Robots { get; }
        IScriptRepository Scripts { get; }
        IEnvironments Environments { get; }
        ILocalRepository<Parameter> Parameters { get; }

        IEnumerable<Uri> GetConfiguredRemoteAddresses();
        void EnsureCanConfigure(string alias);
        Environment ConfigureEnvironment(string alias, Region region, CreatedSession session, ServerConfig server, PropertyOptions properties);
        Task SaveAsync(CancellationToken cancellation);
    }
}