using Joba.Xunit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using static Joba.IBM.RPA.Cli.RobotCommand.NewBotCommand;

namespace Joba.IBM.RPA.Cli.Tests
{
    [UsesVerify]
    public class NewRobotHandlerShould : RequireProjectTest
    {
        private readonly ILogger logger;

        public NewRobotHandlerShould(ITestOutputHelper output) => logger = new XunitLogger(output);

        [Fact]
        public async Task ThrowException_WhenBotAlreadyExists()
        {
            //arrange
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}")));
            var project = await LoadProjectAsync(workingDir);
            var sut = new NewRobotHandler(logger, project);

            //assert
            _ = await Assert.ThrowsAsync<ProjectException>(
                async () => await sut.HandleAsync(new WalFileName("Assistant"), "unattended", CancellationToken.None));
        }

        [Fact]
        public async Task CreateWalAndUpdateProjectFile()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateWalAndUpdateProjectFile))));
            workingDir.Create();
            try
            {
                //arrange
                var project = await LoadProjectAsync(workingDir);
                var sut = new NewRobotHandler(logger, project);

                //act
                await sut.HandleAsync(new WalFileName(nameof(CreateWalAndUpdateProjectFile)), "unattended", CancellationToken.None);

                //assert
                Assert.True(
                    File.Exists(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateWalAndUpdateProjectFile), $"{nameof(CreateWalAndUpdateProjectFile)}.wal"))));
                await VerifyFile(Path.Combine(workingDir.FullName, $"{project.Name}.rpa.json"))
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateWalAndUpdateProjectFile))))
                   .UseFileName($"{project.Name}.rpa");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }
    }
}