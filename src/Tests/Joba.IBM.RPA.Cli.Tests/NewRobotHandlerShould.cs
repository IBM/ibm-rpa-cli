using Joba.Xunit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using static Joba.IBM.RPA.Cli.RobotCommand.NewBotCommand;

namespace Joba.IBM.RPA.Cli.Tests
{
    [UsesVerify]
    [Trait("Category", "Integration")]
    public class NewRobotHandlerShould : RequireProjectTest
    {
        private readonly ILogger logger;

        public NewRobotHandlerShould(ITestOutputHelper output) => logger = new XunitLogger(output);

        [Fact]
        public async Task ThrowException_WhenBotAlreadyExists()
        {
            //arrange
            var properties = new PropertyOptions();
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}")));
            var project = await LoadProjectAsync(workingDir);
            var sut = new NewRobotHandler(logger, project);

            //assert
            _ = await Assert.ThrowsAsync<ProjectException>(
                async () => await sut.HandleAsync("Assistant", "unattended", properties, CancellationToken.None));
        }

        [Fact]
        public async Task CreateAttended()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateAttended))));
            workingDir.Create();
            try
            {
                //arrange
                var properties = new PropertyOptions();
                var project = await LoadProjectAsync(workingDir);
                var sut = new NewRobotHandler(logger, project);

                //act
                await sut.HandleAsync(nameof(CreateAttended), "attended", properties, CancellationToken.None);

                //assert
                var filePath = Path.Combine(project.WorkingDirectory.FullName, $"{nameof(CreateAttended)}.wal");
                Assert.True(File.Exists(filePath), $"File {filePath} should have been created");
                await VerifyFile(Path.Combine(workingDir.FullName, $"{project.Name}.rpa.json"))
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateAttended))))
                   .UseFileName($"{project.Name}.rpa");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }

        [Fact]
        public async Task CreateUnattended()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateUnattended))));
            workingDir.Create();
            try
            {
                //arrange
                var properties = PropertyOptions.Parse("computer-group=MyComputerGroup");
                var project = await LoadProjectAsync(workingDir);
                var sut = new NewRobotHandler(logger, project);

                //act
                await sut.HandleAsync(nameof(CreateUnattended), "unattended", properties, CancellationToken.None);

                //assert
                var filePath = Path.Combine(project.WorkingDirectory.FullName, $"{nameof(CreateUnattended)}.wal");
                Assert.True(File.Exists(filePath), $"File {filePath} should have been created");
                await VerifyFile(Path.Combine(workingDir.FullName, $"{project.Name}.rpa.json"))
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateUnattended))))
                   .UseFileName($"{project.Name}.rpa");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }

        [Fact]
        public async Task CreateChatbot()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateChatbot))));
            workingDir.Create();
            try
            {
                //arrange
                var properties = PropertyOptions.Parse("handle=ChatHandle", "name=ChatName", "computers=Computer1,Computer2", "greeting=I am warming up...");
                var project = await LoadProjectAsync(workingDir);
                var sut = new NewRobotHandler(logger, project);

                //act
                await sut.HandleAsync(nameof(CreateChatbot), "chatbot", properties, CancellationToken.None);

                //assert
                var filePath = Path.Combine(project.WorkingDirectory.FullName, $"{nameof(CreateChatbot)}.wal");
                Assert.True(File.Exists(filePath), $"File {filePath} should have been created");
                await VerifyFile(Path.Combine(workingDir.FullName, $"{project.Name}.rpa.json"))
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewRobotHandlerShould)}", nameof(CreateChatbot))))
                   .UseFileName($"{project.Name}.rpa");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }
    }
}