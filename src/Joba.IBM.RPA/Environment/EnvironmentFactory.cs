
namespace Joba.IBM.RPA
{
    internal static class EnvironmentFactory
    {
        internal static Environment Create(ProjectFile projectFile, string alias, Region region, CreatedSession session)
        {
            var envDir = new DirectoryInfo(Path.Combine(projectFile.WorkingDirectory.FullName, session.TenantName));
            var remote = RemoteSettings.Create(region, session);
            var userFile = new UserSettingsFile(projectFile.ProjectName, alias);
            var userSettings = new UserSettings { Session = Session.From(session) };

            var dependenciesFile = new EnvironmentDependenciesFile(envDir, projectFile.ProjectName, alias);
            return new Environment(alias, envDir, remote, userFile, userSettings, dependenciesFile);
        }

        internal static async Task<Environment> LoadAsync(ProjectFile projectFile, ProjectSettings projectSettings, CancellationToken cancellation)
        {
            var alias = projectSettings.CurrentEnvironment;
            if (string.IsNullOrEmpty(alias))
                throw new EnvironmentException($"No current environment is set for the project.");
            if (!projectSettings.Environments.ContainsKey(alias))
                throw new EnvironmentException($"Environment '{alias}' is not mapped to the project environments.");

            var envDir = projectSettings.GetDirectory(alias, projectFile.WorkingDirectory);
            var (userFile, userSettings) = await UserSettingsFile.LoadAsync(projectFile.ProjectName, alias, cancellation);
            var (dependenciesFile, dependencies) = await EnvironmentDependenciesFile.TryLoadAsync(envDir, projectFile.ProjectName, alias, cancellation);

            return new Environment(alias, envDir, projectSettings.Environments[alias], userFile, userSettings, dependenciesFile, dependencies);
        }
    }
}
