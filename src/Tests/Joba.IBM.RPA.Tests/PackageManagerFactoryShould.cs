using Moq;

namespace Joba.IBM.RPA.Tests
{
    public class PackageManagerFactoryShould
    {
        [Fact]
        public void ThrowException_WhenPackageSourceIsNotConfigured()
        {
            //arrange
            var sourceAlias = "dev";
            var packageSources = new Mock<IPackageSources>();
            packageSources.Setup(p => p.Get(sourceAlias)).Returns((PackageSource?)null);
            var project = new Mock<IProject>();
            project.SetupGet(p => p.PackageSources).Returns(packageSources.Object);
            var clientFactory = new Mock<IRpaClientFactory>();
            var sut = (IPackageManagerFactory)new PackageManagerFactory(clientFactory.Object);

            //assert
            Assert.Throws<PackageSourceNotFoundException>(() => sut.Create(project.Object, sourceAlias));
        }
    }
}
