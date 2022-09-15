using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public partial class DeployService
    {
        class DeleteStaging : DefaultPipelineMiddleware<StagingContext>
        {
            private readonly ILogger logger;

            public DeleteStaging(ILogger logger) => this.logger = logger;

            protected override Task Run(StagingContext context, CancellationToken cancellation)
            {
#if !DEBUG
                logger.LogDebug("Deleting {Directory}", context.Directory);
                context.Directory.Delete(recursive: true);
#endif
                return Task.CompletedTask;
            }
        }
    }
}
