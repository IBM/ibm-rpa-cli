using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public partial class DeployService
    {
        class PublishFiles : DefaultPipelineMiddleware<StagingContext>
        {
            private readonly ILogger logger;

            public PublishFiles(ILogger logger) => this.logger = logger;

            protected override async Task Run(StagingContext context, CancellationToken cancellation)
            {
                if (context.Staging == null)
                    throw new InvalidOperationException("The 'staging' should have been created.");

                var files = context.Staging.Files.Concat(context.Staging.Dependencies.Packages).ToArray();
                var tasks = files.Select(wal =>
                {
                    logger.LogInformation("({Name}) publishing file {CurrentVersion} -> {NextVersion}", wal.Name, wal.Version ?? new WalVersion(0), wal.NextVersion);
                    var model = wal.PrepareToPublish($"New version from {context.Project.Name} project");
                    return context.Client.Script.PublishAsync(model, cancellation)
                        .ContinueWith(t => wal.Overwrite(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
                }).ToArray();

                await Task.WhenAll(tasks);
            }
        }
    }
}
