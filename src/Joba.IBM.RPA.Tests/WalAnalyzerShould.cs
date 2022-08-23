namespace Joba.IBM.RPA.Tests
{
    [UsesVerify]
    public class WalAnalyzerShould
    {
        [Fact]
        public Task FindAndReplaceTwoPackages_OneWithVersion_OneWithoutVersion()
        {
            var directoryName = "assets";
            var fileName = "Assistant_OrangeHRM.txt";
            var packageVersion = new WalVersion(30);
            var wal = new WalContent(File.ReadAllText($"{directoryName}/{fileName}"));

            var parser = new WalParser(wal);
            var lines = parser.Parse();

            var analyzer = new WalAnalyzer(lines);
            var references = analyzer.FindPackages("Joba_OrangeHRM");
            references.Replace(packageVersion);

            var content = lines.Build();
            return Verify(content.ToString())
                .UseDirectory(directoryName)
                .UseFileName(Path.GetFileNameWithoutExtension(fileName));
        }
    }
}