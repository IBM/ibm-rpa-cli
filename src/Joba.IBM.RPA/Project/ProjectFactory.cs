namespace Joba.IBM.RPA
{
    public static class ProjectFactory
    {
        public static Project CreateFromCurrentDirectory(string name, NamePattern pattern)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var projectFile = new ProjectFile(workingDir, name);
            if (projectFile.RpaDirectory.Exists /*&& projectFile.RpaDirectory.EnumerateFileSystemInfos().Any()*/)
                throw new Exception($"A project is already configured in the '{workingDir.FullName}' directory");
            projectFile.RpaDirectory.CreateHidden();

            var userFile = new UserSettingsFile(projectFile.ProjectName);
            var packageSourcesFile = new PackageSourcesFile(workingDir, projectFile.ProjectName);
            var projectSettings = new ProjectSettings();
            projectSettings.Configure(pattern); //Note: by default, add the project 'pattern' to all the dependencies

            return new Project(projectFile, projectSettings, userFile, new UserSettings(), packageSourcesFile);
        }

        public static async Task<Project?> TryLoadFromCurrentDirectoryAsync(CancellationToken cancellation)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var (projectFile, projectSettings) = await ProjectFile.TryLoadAsync(workingDir, cancellation);
            if (projectFile == null || projectSettings == null)
                return null;

            var (userFile, userSettings) = await UserSettingsFile.LoadAsync(projectFile.Value.ProjectName, cancellation);
            var (packageSourcesFile, packageSources) = await PackageSourcesFile.TryLoadAsync(workingDir, projectFile.Value.ProjectName,
                projectSettings, userFile, userSettings, cancellation);

            return new Project(projectFile.Value, projectSettings,
                userFile, userSettings,
                packageSourcesFile, packageSources);
        }
    }
}
