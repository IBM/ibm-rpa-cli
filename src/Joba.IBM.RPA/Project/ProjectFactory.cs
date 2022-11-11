using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public static class ProjectFactory
    {
        public static IProject Create(ILogger logger, DirectoryInfo workingDir, string name)
        {
            var projectFile = new ProjectFile(workingDir, name);
            if (projectFile.RpaDirectory.Exists)
                throw new Exception($"A project is already configured in the '{workingDir.FullName}' directory");
            projectFile.RpaDirectory.CreateHidden();

            var userFile = new UserSettingsFile(projectFile.ProjectName);
            var packageSourcesFile = new PackageSourcesFile(workingDir, projectFile.ProjectName);
            var projectSettings = new ProjectSettings(workingDir);

            return new Project(logger, projectFile, projectSettings, userFile, new UserSettings(), packageSourcesFile);
        }

        public static IProject CreateFromCurrentDirectory(ILogger logger, string name)
            => Create(logger, new DirectoryInfo(System.Environment.CurrentDirectory), name);

        public static async Task<IProject?> TryLoadAsync(ILogger logger, DirectoryInfo workingDir, CancellationToken cancellation)
        {
            var (projectFile, projectSettings) = await ProjectFile.TryLoadAsync(workingDir, cancellation);
            if (projectFile == null || projectSettings == null)
                return null;

            var (userFile, userSettings) = await UserSettingsFile.LoadAsync(projectFile.Value.ProjectName, cancellation);
            var (packageSourcesFile, packageSources) = await PackageSourcesFile.TryLoadAsync(workingDir, projectFile.Value.ProjectName,
                projectSettings, userFile, userSettings, cancellation);

            return new Project(logger, projectFile.Value, projectSettings,
                userFile, userSettings, packageSourcesFile, packageSources);
        }

        public static Task<IProject?> TryLoadFromCurrentDirectoryAsync(ILogger logger, CancellationToken cancellation)
            => TryLoadAsync(logger, new DirectoryInfo(System.Environment.CurrentDirectory), cancellation);
    }
}
