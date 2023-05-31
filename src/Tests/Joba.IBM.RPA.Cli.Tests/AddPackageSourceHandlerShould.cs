using Joba.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System.CommandLine;
using Xunit.Abstractions;
using static Joba.IBM.RPA.Cli.PackageCommand;

namespace Joba.IBM.RPA.Cli.Tests
{
    [UsesVerify]
    [Trait("Category", "Integration")]
    public class AddPackageSourceHandlerShould : RequireProjectTest
    {
        private readonly ILogger logger;

        public AddPackageSourceHandlerShould(ITestOutputHelper output) => logger = new XunitLogger(output);

        [Fact]
        public async Task ThrowException_WhenPackageSourceAlreadyExists()
        {
            //arrange
            var console = new Mock<IConsole>();
            var clientFactory = new Mock<IRpaClientFactory>();
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(ThrowException_WhenPackageSourceAlreadyExists))));
            var project = await LoadProjectAsync(workingDir);
            var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider());
            var options = new RemoteOptions("dev", new ServerAddress());

            //assert
            _ = await Assert.ThrowsAsync<ProjectException>(
                async () => await sut.HandleAsync(options, CancellationToken.None));
        }

        [Fact]
        public async Task ThrowException_WhenEnvironmentAlreadyExists()
        {
            //arrange
            var console = new Mock<IConsole>();
            var clientFactory = new Mock<IRpaClientFactory>();
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(ThrowException_WhenEnvironmentAlreadyExists))));
            var project = await LoadProjectAsync(workingDir);
            var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider());
            var options = new RemoteOptions("dev", new ServerAddress());

            //assert
            _ = await Assert.ThrowsAsync<ProjectException>(
                async () => await sut.HandleAsync(options, CancellationToken.None));
        }

        [Fact]
        public async Task CreatePackageSourcesFile()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(CreatePackageSourcesFile))));
            workingDir.Create();
            try
            {
                //arrange
                var options = new RemoteOptions("dev", new ServerAddress("https://dev.ibm.com"), "us1", "username", 500, "password");
                var region = new Region(options.RegionName!, options.Address.ToUri(), options.Address.ToUri());
                var config = new ServerConfig { Regions = new Region[] { region }, Version = RpaCommand.SupportedServerVersion, Deployment = DeploymentOption.SaaS, AuthenticationMethod = AuthenticationMethod.WDG };
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
                var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider());

                //act
                await sut.HandleAsync(options, CancellationToken.None);

                //assert
                var filePath = Path.Combine(workingDir.FullName, $"{project.Name}.sources.json");
                Assert.True(File.Exists(filePath), $"File {filePath} should have been created");
                await VerifyFile(filePath)
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(CreatePackageSourcesFile))))
                   .UseFileName($"{project.Name}.sources");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }

        [Fact]
        public async Task UpdatePackageSourcesFile()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(UpdatePackageSourcesFile))));
            workingDir.Create();
            try
            {
                //arrange
                var options = new RemoteOptions("qa", new ServerAddress("https://qa.ibm.com"), "us1qa", "username", 501, "password");
                var region = new Region(options.RegionName!, options.Address.ToUri(), options.Address.ToUri());
                var config = new ServerConfig { Regions = new Region[] { region }, Version = RpaCommand.SupportedServerVersion, Deployment = DeploymentOption.SaaS, AuthenticationMethod = AuthenticationMethod.WDG };
                var session = new CreatedSession { TenantCode = options.TenantCode!.Value, TenantName = "quality assurance" };
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
                var sut = new AddPackageSourceHandler(logger, project, console.Object, clientFactory.Object, new DefaultSecretProvider());

                //act
                await sut.HandleAsync(options, CancellationToken.None);

                //assert
                await VerifyFile(Path.Combine(workingDir.FullName, $"{project.Name}.sources.json"))
                   .UseDirectory(Path.GetFullPath(Path.Combine("assets", $"{nameof(AddPackageSourceHandlerShould)}", nameof(UpdatePackageSourcesFile))))
                   .UseFileName($"{project.Name}.sources");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }
    }
}