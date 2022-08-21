
namespace Joba.IBM.RPA
{
    internal static class EnvironmentFactory
    {
        public static Environment Create(ProjectFile projectFile, string alias, Region region, Session session)
        {
            var envDir = new DirectoryInfo(Path.Combine(projectFile.WorkingDirectory.FullName, session.TenantName));
            var userFile = new UserSettingsFile(projectFile.ProjectName, alias);
            var userSettings = new UserSettings { Token = session.AccessToken };

            var envFile = new EnvironmentFile(projectFile.RpaDirectory, projectFile.ProjectName, alias);
            var remote = RemoteSettings.Create(region, session);
            var dependenciesFile = new DependenciesFile(envDir, projectFile.ProjectName, alias);
            var isDefault = projectFile.RpaDirectory.EnumerateFiles($"*{EnvironmentFile.Extension}", SearchOption.TopDirectoryOnly).Any() == false;

            return new Environment(isDefault, envDir, envFile, remote, userFile, userSettings, dependenciesFile, null);
        }

        public static async Task<Environment?> LoadAsync(
            DirectoryInfo rpaDir, ProjectFile projectFile, ProjectSettings projectSettings, CancellationToken cancellation)
        {
            var alias = projectSettings.CurrentEnvironment;
            if (string.IsNullOrEmpty(alias))
                return null;

            var envDir = projectSettings.GetDirectory(alias);
            var (envFile, envSettings) = await EnvironmentFile.LoadAsync(rpaDir, projectFile.ProjectName, alias, cancellation);
            var (userFile, userSettings) = await UserSettingsFile.LoadAsync(projectFile.ProjectName, alias, cancellation);
            var (dependenciesFile, dependencies) = await DependenciesFile.LoadAsync(envDir, projectFile.ProjectName, alias, cancellation);

            return new Environment(envDir, envFile, envSettings, userFile, userSettings, dependenciesFile, dependencies);
        }
    }
}
