namespace Joba.IBM.RPA
{
    public class Project
    {
        private readonly ProjectFile projectFile;
        private readonly ProjectSettings projectSettings;

        internal Project(ProjectFile projectFile, ProjectSettings projectSettings)
        {
            this.projectFile = projectFile;
            this.projectSettings = projectSettings;
        }

        public string Name => projectFile.ProjectName;
        public IProjectDependencies Dependencies => projectSettings.Dependencies;
        public INames Files => projectSettings.Files;

        public async Task SaveAsync(CancellationToken cancellation) =>
            await projectFile.SaveAsync(projectSettings, cancellation);

        public async Task<Environment> ConfigureEnvironmentAndSwitchAsync(IAccountResource resource, string alias, 
            Region region, AccountCredentials credentials, CancellationToken cancellation)
        {
            var session = await credentials.AuthenticateAsync(resource, cancellation);
            var environment = EnvironmentFactory.Create(projectFile, alias, region, session);
            projectSettings.MapEnvironment(alias, environment.Remote);
            SwitchTo(environment.Alias);

            return environment;
        }

        public async Task<Environment?> GetCurrentEnvironmentAsync(CancellationToken cancellation) =>
            await EnvironmentFactory.LoadAsync(projectFile, projectSettings, cancellation);

        public bool SwitchTo(string alias)
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
    }
}