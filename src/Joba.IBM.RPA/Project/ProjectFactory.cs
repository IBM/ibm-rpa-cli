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

            var settings = new ProjectSettings();
            settings.Configure(pattern); //Note: by default, add the project 'pattern' to all the dependencies

            return new Project(projectFile, settings);
        }

        public static async Task<Project?> TryLoadFromCurrentDirectoryAsync(CancellationToken cancellation)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var (projectFile, projectSettings) = await ProjectFile.TryLoadAsync(workingDir, cancellation);
            if (projectFile == null || projectSettings == null)
                return null;

            return new Project(projectFile.Value, projectSettings);
        }
    }
}
