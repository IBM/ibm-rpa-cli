using System.Runtime.CompilerServices;

namespace Joba.IBM.RPA.Cli.Tests
{
    public abstract class RequireProjectTest
    {
        protected async Task<IProject> LoadProjectAsync(DirectoryInfo workingDir, [CallerMemberName] string memberName = "")
        {
            var project = await ProjectFactory.TryLoadAsync(workingDir, CancellationToken.None);
            return project ?? throw new InvalidOperationException($"Cannot run test {memberName} because no project file has been configured in {workingDir.FullName}");
        }
    }
}