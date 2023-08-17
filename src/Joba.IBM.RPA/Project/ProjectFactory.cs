namespace Joba.IBM.RPA
{
    public static class ProjectFactory
    {
        public static IProject Create(DirectoryInfo workingDir, string name, string? description = null)
        {
            var projectFile = new ProjectFile(workingDir, name);
            if (projectFile.RpaDirectory.Exists)
                throw new ProjectException($"A project is already configured in the '{workingDir.FullName}' directory");
            projectFile.RpaDirectory.CreateHidden();

            var userFile = new UserSettingsFile(projectFile.ProjectName);
            var packageSourcesFile = new PackageSourcesFile(workingDir, projectFile.ProjectName);
            var projectSettings = new ProjectSettings(workingDir) { Description = description ?? name };

            return new Project(projectFile, projectSettings, userFile, new UserSettings(), packageSourcesFile);
        }

        public static IProject CreateFromCurrentDirectory(string name)
            => Create(new DirectoryInfo(System.Environment.CurrentDirectory), name);

        public static async Task<IProject?> TryLoadAsync(DirectoryInfo workingDir, CancellationToken cancellation)
        {
            var (projectFile, projectSettings) = await ProjectFile.TryLoadAsync(workingDir, cancellation);
            if (projectFile == null || projectSettings == null)
                return null;

            var (userFile, userSettings) = await UserSettingsFile.LoadAsync(projectFile.Value.ProjectName, cancellation);
            var (packageSourcesFile, packageSources) = await PackageSourcesFile.TryLoadAsync(workingDir, projectFile.Value.ProjectName,
                projectSettings, userFile, userSettings, cancellation);

            return new Project(projectFile.Value, projectSettings,
                userFile, userSettings, packageSourcesFile, packageSources);
        }

        public static Task<IProject?> TryLoadFromCurrentDirectoryAsync(CancellationToken cancellation)
            => TryLoadAsync(new DirectoryInfo(System.Environment.CurrentDirectory), cancellation);
    }
}
