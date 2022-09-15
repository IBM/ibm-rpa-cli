using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public partial class DeployService
    {
        class CreateStaging : DefaultPipelineMiddleware<StagingContext>
        {
            private readonly ILogger logger;

            public CreateStaging(ILogger logger) => this.logger = logger;

            protected override async Task Run(StagingContext context, CancellationToken cancellation)
            {
                logger.LogDebug("Creating the staging directory: {Directory}", context.Directory);
                context.Directory.CreateHidden();
                logger.LogDebug("Merging the source {Source} configuration to the target {Target} and copying to the {Directory}", context.Source.Alias, context.Target.Alias, context.Directory);
                context.Staging = await context.Source.StageAsync(context.Directory, context.Target, cancellation);
            }
        }
    }
}
