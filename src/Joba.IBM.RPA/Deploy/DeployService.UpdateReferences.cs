using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public partial class DeployService
    {
        class UpdateReferences : DefaultPipelineMiddleware<StagingContext>
        {
            private readonly ILogger logger;

            public UpdateReferences(ILogger logger) => this.logger = logger;

            protected override Task Run(StagingContext context, CancellationToken cancellation)
            {
                if (context.Staging == null)
                    throw new InvalidOperationException("The 'staging' should have been created.");

                var files = context.Staging.Files.Concat(context.Staging.Dependencies.Packages).ToArray();
                var packages = files.ToDictionary(k => k.Name, v => v.NextVersion);
                foreach (var wal in files)
                {
                    var parser = new WalParser(wal.Content);
                    var lines = parser.Parse();
                    var analyzer = new WalAnalyzer(lines);
                    foreach (var pair in packages)
                    {
                        logger.LogInformation("({Name}) Updating {Package} references", wal.Name, pair.Key);
                        var references = analyzer.FindPackages(pair.Key);
                        references.Replace(pair.Value);
                        logger.LogDebug("({Name}) Total of {Total} references of {Package} updated", wal.Name, references.Count(), pair.Key);

                        if (logger.IsEnabled(LogLevel.Trace))
                        {
                            foreach (var reference in references)
                                logger.LogTrace("({Name}) Line {Line}, version {Version}", wal.Name, reference.LineNumber, reference.Version?.ToString() ?? "production");
                        }
                    }

                    var content = lines.Build();
                    wal.Overwrite(content);
                }

                return Task.CompletedTask;
            }
        }
    }
}
