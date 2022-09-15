using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public partial class DeployService
    {
        private readonly ILogger<DeployService> logger;
        private readonly IRpaClientFactory clientFactory;
        private readonly Project project;

        public DeployService(ILogger<DeployService> logger, IRpaClientFactory clientFactory, Project project)
        {
            this.logger = logger;
            this.clientFactory = clientFactory;
            this.project = project;
        }

        public async Task DeployAsync(string? sourceAlias, string targetAlias, CancellationToken cancellation)
        {
            var from = await GetFromEnvironmentAsync(sourceAlias, project, cancellation);
            var to = await project.GetEnvironmentAsync(targetAlias, cancellation);

            using var context = new StagingContext(clientFactory, project, from, to);
            var pipeline = AsyncPipeline<StagingContext>.Create()
                .Add(new CreateStaging(logger))
                .Add(new DownloadFilesFromTarget(logger))
                .Add(new CopyFilesToStaging(logger))
                .Add(new UpdateReferences(logger))
                .Add(new PublishFiles(logger))
                .Add(new PublishParameters(logger))
                .Finally(new DeleteStaging(logger));

            await pipeline.ExecuteAsync(context, cancellation);
        }

        private static async Task<Environment> GetFromEnvironmentAsync(string? sourceAlias, Project project, CancellationToken cancellation)
        {
            var task = string.IsNullOrEmpty(sourceAlias) ?
                project.GetCurrentEnvironmentAsync(cancellation)
                    .ContinueWith(t => t.Result ?? throw new EnvironmentException("The current environment is not set."), TaskContinuationOptions.OnlyOnRanToCompletion) :
                project.GetEnvironmentAsync(sourceAlias, cancellation);

            return await task;
        }

        class StagingContext : IDisposable
        {
            private readonly IRpaClientFactory clientFactory;
            private IRpaClient? stagingClient;

            public StagingContext(IRpaClientFactory clientFactory, Project project, Environment source, Environment target)
            {
                this.clientFactory = clientFactory;
                Project = project;
                Source = source;
                Target = target;
                Directory = new DirectoryInfo(Path.Combine(project.RpaDirectory.FullName, "staging"));
            }

            internal DirectoryInfo Directory { get; }
            internal Project Project { get; }
            internal Environment Source { get; }
            internal Environment Target { get; }
            internal Environment? Staging { get; set; }
            internal IRpaClient Client => stagingClient ??= clientFactory.CreateFromEnvironment(Staging ?? throw new InvalidOperationException("The 'staging' should have been created."));

            void IDisposable.Dispose() => stagingClient?.Dispose();
        }
    }
}
