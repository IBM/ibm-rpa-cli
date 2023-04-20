using Joba.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System.CommandLine;
using Xunit.Abstractions;
using static Joba.IBM.RPA.Cli.EnvironmentCommand.NewEnvironmentCommand;

namespace Joba.IBM.RPA.Cli.Tests
{
    [UsesVerify]
    public class NewEnvironmentHandlerShould : RequireProjectTest
    {
        private readonly ILogger logger;

        public NewEnvironmentHandlerShould(ITestOutputHelper output) => logger = new XunitLogger(output);

        [Fact]
        public async Task ThrowException_WhenEnviornmentAlreadyExists()
        {
            //arrange
            var console = new Mock<IConsole>();
            var clientFactory = new Mock<IRpaClientFactory>();
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewEnvironmentHandlerShould)}", nameof(ThrowException_WhenEnviornmentAlreadyExists))));
            var project = await LoadProjectAsync(workingDir);
            var sut = new NewEnvironmentHandler(logger, project, console.Object, clientFactory.Object);
            var options = new RemoteOptions("dev", new ServerAddress());

            //assert
            _ = await Assert.ThrowsAsync<ProjectException>(
                async () => await sut.HandleAsync(options, CancellationToken.None));
        }

        [Fact]
        public async Task ThrowException_WhenPackageSourceAlreadyExists()
        {
            //arrange
            var console = new Mock<IConsole>();
            var clientFactory = new Mock<IRpaClientFactory>();
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewEnvironmentHandlerShould)}", nameof(ThrowException_WhenPackageSourceAlreadyExists))));
            var project = await LoadProjectAsync(workingDir);
            var sut = new NewEnvironmentHandler(logger, project, console.Object, clientFactory.Object);
            var options = new RemoteOptions("dev", new ServerAddress());

            //assert
            _ = await Assert.ThrowsAsync<ProjectException>(
                async () => await sut.HandleAsync(options, CancellationToken.None));
        }

        [Fact]
        public async Task AddEnvironment()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewEnvironmentHandlerShould)}", nameof(AddEnvironment))));
            workingDir.Create();
            try
            {
                //arrange
                var options = new RemoteOptions("dev", new ServerAddress("https://dev.ibm.com"), "us1", "any", 500, "any");
                var region = new Region(options.RegionName!, options.Address.ToUri());
                var config = new ServerConfig { Regions = new Region[] { region } };
                var session = new CreatedSession { TenantCode = options.TenantCode!.Value, TenantName = "development" };
                var console = new Mock<IConsole>();
                var account = new Mock<IAccountResource>();
                account.Setup(a => a.AuthenticateAsync(options.TenantCode!.Value, options.UserName!, options.Password!, It.IsAny<CancellationToken>())).ReturnsAsync(session);
                var client = new Mock<IRpaClient>();
                client.Setup(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>())).ReturnsAsync(config);
                client.SetupGet(c => c.Account).Returns(account.Object);
                var clientFactory = new Mock<IRpaClientFactory>();
                clientFactory.Setup(c => c.CreateFromAddress(region.ApiAddress)).Returns(client.Object);
                clientFactory.Setup(c => c.CreateFromRegion(region)).Returns(client.Object);
                var project = await LoadProjectAsync(workingDir);
                var sut = new NewEnvironmentHandler(logger, project, console.Object, clientFactory.Object);

                //act
                await sut.HandleAsync(options, CancellationToken.None);

                //assert
                await VerifyFile(Path.Combine(workingDir.FullName, $"{project.Name}.rpa.json"))
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(NewEnvironmentHandlerShould)}", nameof(AddEnvironment))))
                   .UseFileName($"{project.Name}.rpa");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }
    }
}