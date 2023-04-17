using Moq;

namespace Joba.IBM.RPA.Tests
{
    public class PackageManagerShould
    {
        [Fact]
        public async Task ThrowException_WhenInstallingPackageDoesNotExist()
        {
            //arrange
            var packageName = "packageName";
            var packageVersion = new WalVersion(10);
            var packages = new Mock<IPackages>();
            packages.Setup(p => p.GetEnumerator()).Returns(Enumerable.Empty<PackageMetadata>().GetEnumerator());
            var project = new Mock<IProject>();
            project.SetupGet(p => p.Packages).Returns(packages.Object);
            var resource = new Mock<IPackageSourceResource>();
            resource.Setup(r => r.SearchAsync(new NamePattern(packageName), It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<Package>());
            var sut = (IPackageManager)new PackageManager(project.Object, resource.Object);

            //assert
            await Assert.ThrowsAsync<PackageNotFoundException>(() => sut.InstallAsync(packageName, packageVersion, CancellationToken.None));
        }

        [Fact]
        public async Task ThrowException_WhenInstallingPackageIsAlreadyInstalled()
        {
            //arrange
            var packageName = "packageName";
            var packageVersion = new WalVersion(10);
            var packageList = new List<PackageMetadata> { new PackageMetadata(packageName, packageVersion) };
            var packages = new Mock<IPackages>();
            packages.Setup(p => p.Get(packageName)).Returns(packageList[0]);
            packages.Setup(p => p.GetEnumerator()).Returns(packageList.GetEnumerator());
            var project = new Mock<IProject>();
            project.SetupGet(p => p.Packages).Returns(packages.Object);
            var resource = new Mock<IPackageSourceResource>();
            resource.Setup(r => r.SearchAsync(new NamePattern(packageName), It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<Package>());
            var sut = (IPackageManager)new PackageManager(project.Object, resource.Object);

            //assert
            await Assert.ThrowsAsync<PackageAlreadyInstalledException>(() => sut.InstallAsync(packageName, packageVersion, CancellationToken.None));
        }

        [Fact]
        public async Task ThrowException_WhenUpdatingPackageDoesNotExist()
        {
            //arrange
            var packageName = "packageName";
            var packageVersion = new WalVersion(10);
            var packages = new Mock<IPackages>();
            packages.Setup(p => p.GetEnumerator()).Returns(Enumerable.Empty<PackageMetadata>().GetEnumerator());
            var project = new Mock<IProject>();
            project.SetupGet(p => p.Packages).Returns(packages.Object);
            var resource = new Mock<IPackageSourceResource>();
            resource.Setup(r => r.SearchAsync(new NamePattern(packageName), It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<Package>());
            var sut = (IPackageManager)new PackageManager(project.Object, resource.Object);

            //assert
            await Assert.ThrowsAsync<PackageNotFoundException>(() => sut.UpdateAsync(packageName, packageVersion, CancellationToken.None));
        }
    }
}
