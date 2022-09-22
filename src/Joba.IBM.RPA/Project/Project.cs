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

        internal DirectoryInfo RpaDirectory => projectFile.RpaDirectory;
        internal DirectoryInfo WorkingDirectory => projectFile.WorkingDirectory;
        public string Name => projectFile.ProjectName;
        public IProjectDependencies Dependencies => projectSettings.Dependencies;
        public INames Files => projectSettings.Files;
        public IPackageSources PackageSources => packageSources ??= new PackageSources(projectSettings, userFile, userSettings);
        public IEnumerable<Uri> GetConfiguredRemoteAddresses() => projectSettings.Environments.Values.Select(v => v.Address).Distinct();

        public void EnsureCanConfigure(string alias)
        {
            if (projectSettings.EnvironmentExists(alias) || (packageSources?.SourceExists(alias)).GetValueOrDefault())
                throw new ProjectException($"Cannot configure '{alias}' because it's already being used. Aliases need to be unique among environments and package sources.");
        }

        /// <summary>
        /// TODO: safely save by saving as ~name and then overwriting the original file
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
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
                : await EnvironmentFactory.LoadAsync(projectSettings.CurrentEnvironment, projectFile, projectSettings, userFile, userSettings, cancellation);

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
            var environment = (await GetCurrentEnvironmentAsync(cancellation))!;

            return (switched, environment);
        }

        public async Task<Environment> GetEnvironmentAsync(string alias, CancellationToken cancellation) =>
            await EnvironmentFactory.LoadAsync(alias, projectFile, projectSettings, userFile, userSettings, cancellation);
    }
}