using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public sealed partial class Bluewasher
    {
        class CopyOriginalFile : DefaultPipelineMiddleware<BuildRobotContext>
        {
            private readonly ILogger logger;

            internal CopyOriginalFile(ILogger logger)
            {
                this.logger = logger;
            }

            protected override Task Run(BuildRobotContext context, CancellationToken cancellation)
            {
                var path = Path.Combine(context.OutputDirectory.FullName, context.OriginalFile.Info.Name);
                logger.LogDebug("Copying {Source} to {Target}", context.OriginalFile.Info.FullName, path);
                File.Copy(context.OriginalFile.Info.FullName, path, true);
                context.File = WalFile.Read(new FileInfo(path));

                return Task.CompletedTask;
            }
        }
    }
}
