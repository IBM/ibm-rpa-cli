using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public partial class DeployService
    {
        class PublishParameters : DefaultPipelineMiddleware<StagingContext>
        {
            private readonly ILogger logger;

            public PublishParameters(ILogger logger) => this.logger = logger;

            protected override async Task Run(StagingContext context, CancellationToken cancellation)
            {
                if (context.Staging == null)
                    throw new InvalidOperationException("The 'staging' should have been created.");

                var tasks = context.Staging.Dependencies.Parameters.Select(parameter =>
                {
                    logger.LogInformation("({Project}) publishing parameter {Name}", context.Project.Name, parameter.Name);
                    return context.Client.Parameter.CreateOrUpdateAsync(parameter.Name, parameter.Value, cancellation);
                }).ToArray();

                await Task.WhenAll(tasks);
            }
        }
    }
}
