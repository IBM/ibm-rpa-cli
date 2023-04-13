using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Joba.IBM.RPA.Tests
{
    //TODO: these do not pass on CI and are prone to error because of how ZIP works.
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
            var robotList = new List<Robot> { robot };
            var robots = new Mock<IRobots>();
            robots.Setup(r => r.GetEnumerator()).Returns(robotList.GetEnumerator());

            var project = new Mock<IProject>();
            project.Setup(p => p.Name).Returns("test");
            project.Setup(p => p.WorkingDirectory).Returns(workingDir);
            project.Setup(p => p.Scripts).Returns(new ScriptRepository(workingDir));
            project.Setup(p => p.Robots).Returns(robots.Object);
            var packageManagerFactory = new Mock<IPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.Create(project.Object, null)).Returns(new Mock<IPackageManager>().Object);
            var arguments = new BuildArguments(project.Object, robot, outputDir);
            var sut = (ICompiler)new Bluewasher(logger, packageManagerFactory.Object);

            //act
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
            var robotList = new List<Robot> { robot };
            var robots = new Mock<IRobots>();
            robots.Setup(r => r.GetEnumerator()).Returns(robotList.GetEnumerator());

            var project = new Mock<IProject>();
            project.Setup(p => p.Name).Returns("test");
            project.Setup(p => p.WorkingDirectory).Returns(workingDir);
            project.Setup(p => p.Scripts).Returns(new ScriptRepository(workingDir));
            project.Setup(p => p.Robots).Returns(robots.Object);
            var packageManagerFactory = new Mock<IPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.Create(project.Object, null)).Returns(new Mock<IPackageManager>().Object);
            var arguments = new BuildArguments(project.Object, robot, outputDir);
            var sut = (ICompiler)new Bluewasher(logger, packageManagerFactory.Object);

            //act
            var result = await sut.BuildAsync(arguments, CancellationToken.None);

            //assert
            Assert.True(result.Success, result.Error?.ToString());
            var expected = WalFile.ReadAllText(new FileInfo(Path.GetFullPath(@"assets/build/02/expected.wal")));
            Assert.Equal(expected, result.Robots[robot].Content);
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
            var robotList = new List<Robot> { unattendedBot, attendedBot, chatbot };
            var robots = new Mock<IRobots>();
            robots.Setup(r => r.GetEnumerator()).Returns(robotList.GetEnumerator());

            var project = new Mock<IProject>();
            project.Setup(p => p.Name).Returns("test");
            project.Setup(p => p.WorkingDirectory).Returns(workingDir);
            project.Setup(p => p.Scripts).Returns(new ScriptRepository(workingDir));
            project.Setup(p => p.Robots).Returns(robots.Object);
            var packageManagerFactory = new Mock<IPackageManagerFactory>();
            packageManagerFactory.Setup(p => p.Create(project.Object, null)).Returns(new Mock<IPackageManager>().Object);
            var arguments = new BuildArguments(project.Object, outputDir);
            var sut = (ICompiler)new Bluewasher(logger, packageManagerFactory.Object);

            //act
            var result = await sut.BuildAsync(arguments, CancellationToken.None);

            //assert
            Assert.True(result.Success, result.Error?.ToString());
            Assert.Equal(robotList.Count, result.Robots.Count);

            var expectedUnattended = WalFile.ReadAllText(new FileInfo(Path.GetFullPath(@"assets/build/03/expected-unattended.wal")));
            var expectedAttended = WalFile.ReadAllText(new FileInfo(Path.GetFullPath(@"assets/build/03/expected-attended.wal")));
            var expectedChatbot = WalFile.ReadAllText(new FileInfo(Path.GetFullPath(@"assets/build/03/expected-chatbot.wal")));

            Assert.Equal(expectedUnattended, result.Robots[unattendedBot].Content);
            Assert.Equal(expectedAttended, result.Robots[attendedBot].Content);
            Assert.Equal(expectedChatbot, result.Robots[chatbot].Content);
        }
    }

    class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper output;

        public XunitLogger(ITestOutputHelper output) => this.output = output;

        IDisposable ILogger.BeginScope<TState>(TState state) => new DummyScope();

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            output.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        }

        class DummyScope : IDisposable
        {
            void IDisposable.Dispose()
            {
            }
        }
    }
}