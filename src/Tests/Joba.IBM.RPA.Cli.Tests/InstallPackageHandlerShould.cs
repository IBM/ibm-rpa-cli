using Joba.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;
using static Joba.IBM.RPA.Cli.PackageCommand;

namespace Joba.IBM.RPA.Cli.Tests
{
    [UsesVerify]
    public class InstallPackageHandlerShould : RequireProjectTest
    {
        private readonly ILogger logger;

        public InstallPackageHandlerShould(ITestOutputHelper output) => logger = new XunitLogger(output);

        [Fact]
        public async Task ThrowException_WhenWilcardAndVersionIsSpecified()
        {
            //arrange
            var name = "package*";
            var sourceAlias = "any";
            var version = 10;
            var packageManagerFactory = new Mock<IPackageManagerFactory>();
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(InstallPackageHandlerShould)}", nameof(ThrowException_WhenWilcardAndVersionIsSpecified))));
            var project = await LoadProjectAsync(workingDir);
            var sut = new InstallPackageHandler(logger, project, packageManagerFactory.Object);

            //assert
            _ = await Assert.ThrowsAsync<PackageException>(
                async () => await sut.HandleAsync(name, version, sourceAlias, CancellationToken.None));
        }
    }
}