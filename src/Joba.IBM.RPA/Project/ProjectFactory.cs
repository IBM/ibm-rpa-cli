namespace Joba.IBM.RPA
{
    public static class ProjectFactory
    {
        public static Project CreateFromCurrentDirectory(string name, NamePattern pattern)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var projectFile = new ProjectFile(workingDir, name);
            if (projectFile.RpaDirectory.Exists)
                throw new Exception($"A project is already configured in the '{workingDir.FullName}' directory");
            projectFile.RpaDirectory.CreateHidden();
            
            var settings = new ProjectSettings(pattern);
            settings.Dependencies.Configure(pattern); //by default, add the project 'pattern' to all the dependencies

            return new Project(projectFile, settings);
        }

        public static async Task<Project> LoadFromCurrentDirectoryAsync(CancellationToken cancellation)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var (projectFile, projectSettings) = await ProjectFile.LoadAsync(workingDir, cancellation);
            return new Project(projectFile, projectSettings);
        }
    }
}
