using Joba.Xunit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using static Joba.IBM.RPA.Cli.ProjectCommand;

namespace Joba.IBM.RPA.Cli.Tests
{
    [UsesVerify]
    [Trait("Category", "Integration")]
    public class NewProjectHandlerShould
    {
        private readonly ILogger logger;

        public NewProjectHandlerShould(ITestOutputHelper output) => logger = new XunitLogger(output);

        [Fact]
        public async Task CreateProjectJsonFile()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(nameof(CreateProjectJsonFile)));
            workingDir.Create();
            try
            {
                //arrange
                var projectName = nameof(CreateProjectJsonFile);
                var sut = new NewProjectHandler(logger);

                //act
                await sut.HandleAsync(workingDir, projectName, null, CancellationToken.None);

                //assert
                await VerifyFile(Path.Combine(workingDir.FullName, $"{projectName}.rpa.json"))
                   .UseDirectory(Path.GetFullPath("assets"))
                   .UseFileName($"{projectName}.rpa");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }

        [Fact]
        public async Task ThrowException_WhenAnyProjectIsAlreadyConfiguredInTheWorkingDirectory()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(nameof(ThrowException_WhenAnyProjectIsAlreadyConfiguredInTheWorkingDirectory)));
            workingDir.Create();
            try
            {
                //arrange
                var projectName = nameof(ThrowException_WhenAnyProjectIsAlreadyConfiguredInTheWorkingDirectory);
                var sut = new NewProjectHandler(logger);
                await sut.HandleAsync(workingDir, projectName, null, CancellationToken.None);

                //assert
                _ = await Assert.ThrowsAsync<ProjectException>(async () => await sut.HandleAsync(workingDir, projectName, null, CancellationToken.None));
            }
            finally
            {
                workingDir.Delete(true);
            }
        }
    }
}