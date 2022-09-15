
namespace Joba.IBM.RPA
{
    internal static class EnvironmentFactory
    {
        internal static Environment Create(ProjectFile projectFile, UserSettingsFile userFile, UserSettings userSettings,
            string alias, Region region, CreatedSession session)
        {
            var envDir = new DirectoryInfo(Path.Combine(projectFile.WorkingDirectory.FullName, session.TenantName));
            var remote = RemoteSettings.Create(region, session);
            userSettings.Sessions.Add(alias, Session.From(session));

            var dependenciesFile = new EnvironmentDependenciesFile(envDir, projectFile.ProjectName, alias);
            return new Environment(alias, envDir, remote, userFile, userSettings, dependenciesFile);
        }

        internal static async Task<Environment> LoadAsync(string alias, ProjectFile projectFile, ProjectSettings projectSettings,
            UserSettingsFile userFile, UserSettings userSettings, CancellationToken cancellation)
        {
            if (!projectSettings.Environments.ContainsKey(alias))
                throw new EnvironmentException($"Environment '{alias}' is not mapped to the project environments.");

            var envDir = projectSettings.GetDirectory(alias, projectFile.WorkingDirectory);
            var (dependenciesFile, dependencies) = await EnvironmentDependenciesFile.TryLoadAsync(envDir, projectFile.ProjectName, alias, cancellation);

            return new Environment(alias, envDir, projectSettings.Environments[alias], userFile, userSettings, dependenciesFile, dependencies);
        }
    }
}
