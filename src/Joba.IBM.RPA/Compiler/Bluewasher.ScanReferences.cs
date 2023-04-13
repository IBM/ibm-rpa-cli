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
                var references = scanner.Scan().ToList();
                context.Dependencies = references.Select(r => r.Right).Distinct().ToList();

                logger.LogInformation("Total of dependencies found for '{File}': {Total}", context.File.Name, context.Dependencies.Count());
                return Task.CompletedTask;
            }
        }
    }
}
