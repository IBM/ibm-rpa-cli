namespace Joba.IBM.RPA.Cli.Tests
{
    public class TemplateFactoryShould
    {
        [Fact]
        public async Task ThrowException_WhenTemplateDoesNotExist()
        {
            //arrange
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(TemplateFactoryShould)}")));
            var fileName = new WalFileName("any.wal");
            var templateName = "do-not-exist";
            var sut = new TemplateFactory(workingDir, typeof(Program).Assembly);

            //assert
            _ = await Assert.ThrowsAsync<NotSupportedException>(
                async () => await sut.CreateAsync(fileName, templateName, CancellationToken.None));
        }

        [Fact]
        public async Task ThrowException_WhenFileAlreadyExistsInWorkingDirectory()
        {
            //arrange
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(TemplateFactoryShould)}", nameof(ThrowException_WhenFileAlreadyExistsInWorkingDirectory))));
            var fileName = new WalFileName("botname.wal");
            var templateName = "unattended";
            var sut = new TemplateFactory(workingDir, typeof(Program).Assembly);

            //assert
            _ = await Assert.ThrowsAsync<TemplateException>(
                async () => await sut.CreateAsync(fileName, templateName, CancellationToken.None));
        }

        [Fact]
        public async Task CreateFile()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(TemplateFactoryShould)}", nameof(CreateFile))));
            workingDir.Create();
            try
            {
                //arrange
                var fileName = new WalFileName("botname.wal");
                var templateName = "unattended";
                var sut = new TemplateFactory(workingDir, typeof(Program).Assembly);

                //act
                var wal = await sut.CreateAsync(fileName, templateName, CancellationToken.None);

                //assert
                Assert.True(wal.Info.Exists, $"Wal file '{wal.Info.FullName}' should exist");
            }
            finally
            {
                workingDir.Delete(true);
            }
        }

        [Fact]
        public async Task ReplaceWorkingDirectoryVariable()
        {
            var workingDir = new DirectoryInfo(Path.GetFullPath(Path.Combine("assets", $"{nameof(TemplateFactoryShould)}", nameof(ReplaceWorkingDirectoryVariable))));
            workingDir.Create();
            try
            {
                //arrange
                var fileName = new WalFileName("botname.wal");
                var templateName = "unattended";
                var sut = new TemplateFactory(workingDir, typeof(Program).Assembly);

                //act
                var wal = await sut.CreateAsync(fileName, templateName, CancellationToken.None);

                //assert
                var expected = $"\"{workingDir.FullName.Replace("\\", "\\\\")}\"";
                var analyzer = new WalAnalyzer(wal);
                var setVar = analyzer.EnumerateCommands<SetVarIfLine>(SetVarIfLine.Verb).FirstOrDefault(s => s.Name == "${workingDirectory}");
                Assert.True(setVar != null, "setVarIf --name \"${workingDirectory}\" should exist");
                Assert.Equal(expected, setVar.Value);
            }
            finally
            {
                workingDir.Delete(true);
            }
        }
    }
}
