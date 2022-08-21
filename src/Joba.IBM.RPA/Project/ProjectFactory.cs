namespace Joba.IBM.RPA
{
    public static class ProjectFactory
    {
        public static Project CreateFromCurrentDirectory(string name)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var rpaDir = new DirectoryInfo(Constants.GetRpaDirectoryPath(workingDir.FullName));
            if (rpaDir.Exists)
                throw new Exception($"A project is already configured in the '{workingDir.FullName}' directory");

            rpaDir.CreateHidden();
            return new Project(rpaDir, name);
        }

        public static async Task<Project> LoadFromCurrentDirectoryAsync(CancellationToken cancellation)
        {
            var workingDir = new DirectoryInfo(System.Environment.CurrentDirectory);
            var rpaDir = new DirectoryInfo(Constants.GetRpaDirectoryPath(workingDir.FullName));
            if (!rpaDir.Exists)
                throw new Exception($"Could not load project because it does not exist in '{workingDir}'");

            var (projectFile, projectSettings) = await ProjectFile.LoadAsync(rpaDir, cancellation);
            return new Project(projectFile, projectSettings);
        }
    }
}
