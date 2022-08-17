
namespace Joba.IBM.RPA
{
    internal static class EnvironmentFactory
    {
        public static Environment Create(DirectoryInfo workingDir, ProjectFile projectFile,
            string alias, Region region, Session session)
        {
            var envDir = new DirectoryInfo(Path.Combine(workingDir.FullName, session.TenantName));
            var userFile = new UserSettingsFile(projectFile.ProjectName, alias);
            var userSettings = new UserSettings { Token = session.AccessToken };

            var envFile = new EnvironmentFile(projectFile.RpaDirectory, projectFile.ProjectName, alias);
            var remote = RemoteSettings.Create(region, session);
            return new Environment(envDir, envFile, remote, userFile, userSettings);
        }

        public static async Task<Environment?> LoadAsync(
            DirectoryInfo rpaDir, ProjectFile projectFile, ProjectSettings projectSettings, CancellationToken cancellation)
        {
            var alias = projectSettings.CurrentEnvironment;
            if (string.IsNullOrEmpty(alias))
                return null;

            var (envFile, envSettings) = await EnvironmentFile.LoadAsync(rpaDir, projectFile.ProjectName, alias, cancellation);
            var (userFile, userSettings) = await UserSettingsFile.LoadAsync(projectFile.ProjectName, alias, cancellation);
            var envDir = projectSettings.GetDirectory(alias);
            return new Environment(envDir, envFile, envSettings, userFile, userSettings);
        }
    }
}
