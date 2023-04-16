using Joba.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Compression;
using Xunit.Abstractions;

namespace Joba.IBM.RPA.Tests
{
    public class BluewasherShould
    {
        private readonly ILogger logger;

        public BluewasherShould(ITestOutputHelper output) => logger = new XunitLogger(output);

        [Fact]
        public async Task Build_Robot_Without_Dependencies()
        {
            //arrange
            var workingDir = new DirectoryInfo(Path.GetFullPath("assets/build/01/working-directory"));
            var outputDir = new DirectoryInfo(Path.Combine(System.Environment.CurrentDirectory, "out/01"));
            outputDir.Create();

            var robot = new Robot("main", RobotSettingsFactory.Create("unattended"));
            var project = MockProject(workingDir, nameof(Build_Robot_Without_Dependencies), robot);
            var sut = CreateCompiler(logger, project);

            //act
            var arguments = new BuildArguments(project, robot, outputDir);
            var result = await sut.BuildAsync(arguments, CancellationToken.None);

            //assert
            Assert.True(result.Success, result.Error?.ToString());
            var expected = WalFile.ReadAllText(new FileInfo(Path.GetFullPath("assets/build/01/expected.wal")));
            Assert.Equal(expected, result.Robots[robot].Content);
        }

        [Fact]
        public async Task Build_Robot_With_Dependencies()
        {
            //arrange
            var workingDir = new DirectoryInfo(Path.GetFullPath(@"assets/build/02/working-directory"));
            var outputDir = new DirectoryInfo(Path.Combine(System.Environment.CurrentDirectory, @"out/02"));
            outputDir.Create();

            var robot = new Robot("main", RobotSettingsFactory.Create("unattended"));
            var project = MockProject(workingDir, nameof(Build_Robot_With_Dependencies), robot);
            var sut = CreateCompiler(logger, project);

            //act
            var arguments = new BuildArguments(project, robot, outputDir);
            var result = await sut.BuildAsync(arguments, CancellationToken.None);

            //assert
            Assert.True(result.Success, result.Error?.ToString());

            var analyzer = new WalAnalyzer(result.Robots[robot]);
            AssertDefineVariables(analyzer);
            AssertRoutine(analyzer);
            AssertImport(analyzer, "greetings.wal", $"math{Path.DirectorySeparatorChar}multiply.wal",$"math{Path.DirectorySeparatorChar}subtract.wal", $"math{Path.DirectorySeparatorChar}sum.wal");
        }

        [Fact]
        public async Task Build_Project_With_Several_Robots()
        {
            //arrange
            var workingDir = new DirectoryInfo(Path.GetFullPath(@"assets/build/03/working-directory"));
            var outputDir = new DirectoryInfo(Path.Combine(System.Environment.CurrentDirectory, @"out/03"));
            outputDir.Create();

            var unattendedBot = new Robot("unattended", RobotSettingsFactory.Create("unattended"));
            var attendedBot = new Robot("attended", RobotSettingsFactory.Create("attended"));
            var chatbot = new Robot("chatbot", RobotSettingsFactory.Create("chatbot"));
            var project = MockProject(workingDir, nameof(Build_Project_With_Several_Robots), unattendedBot, attendedBot, chatbot);
            var sut = CreateCompiler(logger, project);

            //act
            var arguments = new BuildArguments(project, outputDir);
            var result = await sut.BuildAsync(arguments, CancellationToken.None);

            //assert
            Assert.True(result.Success, result.Error?.ToString());

            var unattendedAnalyzer = new WalAnalyzer(result.Robots[unattendedBot]);
            AssertDefineVariables(unattendedAnalyzer);
            AssertRoutine(unattendedAnalyzer);
            AssertImport(unattendedAnalyzer, "utils.wal");

            var attendedAnalyzer = new WalAnalyzer(result.Robots[attendedBot]);
            AssertDefineVariables(attendedAnalyzer);
            AssertRoutine(attendedAnalyzer);
            AssertImport(attendedAnalyzer, $"packages{Path.DirectorySeparatorChar}system.wal");

            var chatbotAnalyzer = new WalAnalyzer(result.Robots[chatbot]);
            AssertDefineVariables(chatbotAnalyzer);
            AssertRoutine(chatbotAnalyzer);
            AssertImport(chatbotAnalyzer, @"utils.wal");
        }

        private static void AssertImport(WalAnalyzer analyzer, params string[] entriesPaths)
        {
            var imports = analyzer.EnumerateCommands<ImportLine>(ImportLine.Verb).ToList();
            Assert.Single(imports);
            var import = imports.First();

            Assert.Equal("__dependencies", import.Name);
            using var stream = new MemoryStream(Convert.FromBase64String(import.Base64Content));
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (var entryPath in entriesPaths)
            {
                if (zip.GetEntry(entryPath) == null)
                    Assert.Fail($"Embedded zip should contain {entryPath}. Entries: {string.Join(",", zip.Entries.Select(e => e.FullName))}");
            }
        }

        private static void AssertDefineVariables(WalAnalyzer analyzer)
        {
            var defVars = analyzer.EnumerateCommands<DefineVariableLine>(DefineVariableLine.Verb).ToList();
            Assert.Contains(defVars, d => d.Name == "__tempPath");
            Assert.Contains(defVars, d => d.Name == "__dependenciesDir");
            Assert.Contains(defVars, d => d.Name == "__dependenciesFilePath");
        }

        private static void AssertRoutine(WalAnalyzer analyzer)
        {
            var beginSubs = analyzer.EnumerateCommands<BeginSubLine>(BeginSubLine.Verb).ToList();
            Assert.Contains(beginSubs, d => d.Name == "__ExportDependenciesAndSetWorkingDirectory");

            var goSubs = analyzer.EnumerateCommands<GoSubLine>(GoSubLine.Verb).ToList();
            Assert.Contains(goSubs, d => d.Label == "__ExportDependenciesAndSetWorkingDirectory");
        }

        private static IProject MockProject(DirectoryInfo workingDir, string projectName, params Robot[] robots)
        {
            var robotList = robots.ToList();
            var mockRobots = new Mock<IRobots>();
            mockRobots.Setup(r => r.GetEnumerator()).Returns(robotList.GetEnumerator());

            var mockProject = new Mock<IProject>();
            mockProject.Setup(p => p.Name).Returns(projectName);
            mockProject.Setup(p => p.WorkingDirectory).Returns(workingDir);
            mockProject.Setup(p => p.Scripts).Returns(new ScriptRepository(workingDir));
            mockProject.Setup(p => p.Packages).Returns(new PackageReferences(workingDir));
            mockProject.Setup(p => p.Robots).Returns(mockRobots.Object);

            return mockProject.Object;
        }

        private static ICompiler CreateCompiler(ILogger logger, IProject project)
        {
            var packageManagerFactory = new Mock<IPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.Create(project, null)).Returns(new Mock<IPackageManager>().Object);
            return new Bluewasher(logger, packageManagerFactory.Object);
        }
    }
}