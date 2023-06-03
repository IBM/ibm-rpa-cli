using Joba.Xunit;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Joba.IBM.RPA.Cli.Tests
{
    [Trait("Category", "e2e")]
    public class EndToEndTest : IDisposable
    {
        private readonly ILogger logger;
        private readonly DirectoryInfo workingDirectory;

        public EndToEndTest(ITestOutputHelper output)
        {
            logger = new XunitLogger(output);
            workingDirectory = new DirectoryInfo(System.Environment.CurrentDirectory);
        }

        [Fact]
        public async Task CreateAndDeployProject()
        {
            var projectName = $"MyProject-{DateTimeOffset.UtcNow:ddMMyyyyhhmmss}";
            await RunAsync($"project new {projectName}");
            await RunAsync("env new dev --url https://us1api.wdgautomation.com/v1.0/ --region US1 --userName joberto.diniz@ibm.com --tenant 5283");
            await RunAsync("env new qa --url https://ap1qaapi.wdgautomation.com/v1.0/ --region QA_AP1 --userName owner@wdgautomation.com --tenant 5000");
            await RunAsync("package source joba --url https://us1api.wdgautomation.com/v1.0/ --region US1 --userName joberto.diniz@ibm.com --tenant 5547");
            await RunAsync("package install Joba*");
            await RunAsync("pull Assistant* --env dev");
            await RunAsync("bot new Attended --template attended");
            await RunAsync("bot new Assistant --template chatbot");
            //await RunAsync("bot new Unattended --template unattended"); TODO: we would need to setup computer + computer group

            await RunAsync("deploy qa");
        }

        private async Task RunAsync(string arguments)
        {
            var exitCode = await StartProcessAsync(arguments);
            Assert.True(exitCode >= 0);
        }

        private async Task<int> StartProcessAsync(string arguments)
        {
            var envVarName = "RPA_EXECUTABLE_PATH";
            var path = System.Environment.GetEnvironmentVariable(envVarName) ?? throw new InvalidOperationException($"The environment variable {envVarName} was not set.");
            var info = new ProcessStartInfo(Path.GetFullPath(path), $"{arguments} -v Detailed")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(info) ?? throw new Exception($"Could not start '{info.FileName} {info.Arguments}'");
            process.ErrorDataReceived += OnError;
            process.BeginErrorReadLine();
            var output = await process.StandardOutput.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(output))
                logger.LogInformation(output);
            await process.WaitForExitAsync();

            return process.ExitCode;
        }

        private void OnError(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                logger.LogError($"Process error: {e.Data}");
        }

        void IDisposable.Dispose()
        {
            var rpaDir = new DirectoryInfo(Path.Combine(workingDirectory.FullName, ProjectFile.HiddenDirectoryName));
            if (rpaDir.Exists)
                rpaDir.Delete(true);
            var packagesDir = new DirectoryInfo(Path.Combine(workingDirectory.FullName, PackageSourcesFile.PackagesDirectoryName));
            if (packagesDir.Exists)
                packagesDir.Delete(true);

            var projectFiles = workingDirectory.EnumerateFiles(ProjectFile.Extension);
            foreach (var file in projectFiles)
                file.Delete();

            var packageSourceFiles = workingDirectory.EnumerateFiles(PackageSourcesFile.Extension);
            foreach (var file in packageSourceFiles)
                file.Delete();

            var walFiles = workingDirectory.EnumerateFiles(WalFile.Extension);
            foreach (var file in walFiles)
                file.Delete();
        }
    }
}
