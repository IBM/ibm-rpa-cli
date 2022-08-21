namespace Joba.IBM.RPA
{
    public static class ProjectFactory
    {
        public static Project CreateFromCurrentDirectory(string name)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var projectFile = new ProjectFile(workingDir, name);
            if (projectFile.RpaDirectory.Exists)
                throw new Exception($"A project is already configured in the '{workingDir.FullName}' directory");

            projectFile.RpaDirectory.CreateHidden();
            return new Project(projectFile);
        }

        public static async Task<Project> LoadFromCurrentDirectoryAsync(CancellationToken cancellation)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var (projectFile, projectSettings) = await ProjectFile.LoadAsync(workingDir, cancellation);
            return new Project(projectFile, projectSettings);
        }
    }
}
