using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{

    public sealed partial class Bluewasher
    {
        class ScanReferences : DefaultPipelineMiddleware<BuildRobotContext>
        {
            private readonly ILogger logger;

            public ScanReferences(ILogger logger)
            {
                this.logger = logger;
            }

            protected override Task Run(BuildRobotContext context, CancellationToken cancellation)
            {
                if (context.File == null) throw new InvalidOperationException($"Cannot scan references because the file '{context.OriginalFile.Name}' was not copied to the output '{context.OutputDirectory.FullName}'.");

                var scanner = context.CreateScanner(logger);
                var dependencies = scanner.Scan().Select(r => r.Right).Distinct().ToList();
                if (context.Robot.Settings.Include != null)
                {
                    logger.LogDebug("Including explicit dependencies specified in the robot {Robot} setting", context.Robot.Name);
                    dependencies.AddRange(ScanExplicitDependencies(context.Project, context.Robot.Settings.Include, f => dependencies.Contains(f) == false));
                }

                context.Dependencies = dependencies;
                logger.LogInformation("Total dependencies found for '{File}': {Total}", context.File.Name, dependencies.Count);
                return Task.CompletedTask;
            }

            private IEnumerable<FileInfo> ScanExplicitDependencies(IProject project, string[] include, Func<FileInfo, bool> dependencyDoesNotExist)
            {
                var candidates = include.Select(dependency =>
                    new FileInfo(Path.Combine(project.WorkingDirectory.FullName, dependency))).ToList();
                var nonExistingFiles = candidates.Where(e => !e.Exists).ToArray();
                if (nonExistingFiles.Any())
                    throw new InvalidOperationException($"The following explicit dependencies were not found:\n{string.Join(System.Environment.NewLine, nonExistingFiles.Select(n => n.FullName))}");

                var @explicit = candidates.ToList();
                foreach (var wal in candidates.Where(dependencyDoesNotExist).Select(WalFile.Read))
                {
                    var scanner = new ReferenceScanner(logger, project, wal);
                    var dependencies = scanner.Scan().Select(r => r.Right).Distinct();
                    @explicit.AddRange(dependencies);
                }

                return @explicit.DistinctBy(e => e.FullName);
            }
        }
    }
}
