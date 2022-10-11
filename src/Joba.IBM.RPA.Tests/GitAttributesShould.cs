using static Joba.IBM.RPA.Cli.GitConfigurator;

namespace Joba.IBM.RPA.Tests
{
    [UsesVerify]
    public class GitAttributesShould
    {
        [Fact]
        public async Task UpdatePattern()
        {
            //arrange
            var directoryName = "assets/gitattributes";
            var file = new FileInfo($"{directoryName}/{nameof(UpdatePattern)}.txt");
            var gitAttributes = new GitAttributes(file);

            //act
            await gitAttributes.ConfigureAsync(CancellationToken.None);

            //assert
            await VerifyFile(file)
                .UseDirectory(directoryName)
                .UseFileName(Path.GetFileNameWithoutExtension(file.Name));
        }

        [Fact]
        public async Task AddPattern()
        {
            //arrange
            var directoryName = "assets/gitattributes";
            var file = new FileInfo($"{directoryName}/{nameof(AddPattern)}.txt");
            var gitAttributes = new GitAttributes(file);

            //act
            await gitAttributes.ConfigureAsync(CancellationToken.None);

            //assert
            await VerifyFile(file)
                .UseDirectory(directoryName)
                .UseFileName(Path.GetFileNameWithoutExtension(file.Name));
        }
    }
}
