namespace Joba.IBM.RPA
{
    public class Project
    {
        private readonly ProjectFile projectFile;
        private readonly ProjectSettings projectSettings;
        private readonly PackageSourcesFile packageSourcesFile;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
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
        }

        public string Name => projectFile.ProjectName;
        public IProjectDependencies Dependencies => projectSettings.Dependencies;
        public INames Files => projectSettings.Files;
        public IPackageSources PackageSources => packageSources ??= new PackageSources(projectSettings, userFile, userSettings);

        public async Task SaveAsync(CancellationToken cancellation)
        {
            await projectFile.SaveAsync(projectSettings, cancellation);
            await userFile.SaveAsync(userSettings, cancellation);
            if (packageSources != null)
                await packageSourcesFile.SaveAsync(packageSources, cancellation);
        }

        public async Task<Environment> ConfigureEnvironmentAndSwitchAsync(IAccountResource resource, string alias,
            Region region, AccountCredentials credentials, CancellationToken cancellation)
        {
            var session = await credentials.AuthenticateAsync(resource, cancellation);
            var environment = EnvironmentFactory.Create(projectFile, userFile, userSettings, alias, region, session);
            projectSettings.MapEnvironment(alias, environment.Remote);
            SwitchTo(environment.Alias);

            return environment;
        }

        public async Task<Environment?> GetCurrentEnvironmentAsync(CancellationToken cancellation) =>
            string.IsNullOrEmpty(projectSettings.CurrentEnvironment)
                ? null
                : await EnvironmentFactory.LoadAsync(projectFile, projectSettings, userFile, userSettings, cancellation);

        private bool SwitchTo(string alias)
        {
            if (!projectSettings.EnvironmentExists(alias))
                throw new Exception($"The environment '{alias}' does not exist");

            if (projectSettings.CurrentEnvironment == null ||
                !projectSettings.CurrentEnvironment.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
            {
                projectSettings.CurrentEnvironment = alias;
                return true;
            }

            return false;
        }

        public async Task<(bool, Environment)> SwitchToAsync(string alias, CancellationToken cancellation)
        {
            var switched = SwitchTo(alias);
            var environment = await EnvironmentFactory.LoadAsync(projectFile, projectSettings, userFile, userSettings, cancellation);

            return (switched, environment);
        }
    }
}