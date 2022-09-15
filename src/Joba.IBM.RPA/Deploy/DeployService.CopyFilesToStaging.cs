using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public partial class DeployService
    {
        class CopyFilesToStaging : DefaultPipelineMiddleware<StagingContext>
        {
            private readonly ILogger logger;

            public CopyFilesToStaging(ILogger logger) => this.logger = logger;

            protected override Task Run(StagingContext context, CancellationToken cancellation)
            {
                if (context.Staging == null)
                    throw new InvalidOperationException("The 'staging' should have been created.");

                CopySourceToStaging(context.Project, context.Source.Files, context.Staging.Files);
                CopySourceToStaging(context.Project, context.Source.Dependencies.Packages, context.Staging.Dependencies.Packages);

                return Task.CompletedTask;
            }

            private void CopySourceToStaging(Project project, ILocalRepository source, ILocalRepository staging)
            {
                foreach (var sourceFile in source)
                {
                    logger.LogInformation("Copying {Source} to {Target}", Path.GetRelativePath(sourceFile.Info.FullName, project.WorkingDirectory.FullName));
                    var stagingFile = staging.Get(sourceFile.Name);
                    if (stagingFile == null)
                        _ = sourceFile.CopyContentsTo(staging.Directory);
                    else
                        stagingFile.Overwrite(sourceFile.Content);
                }
            }
        }
    }
}
