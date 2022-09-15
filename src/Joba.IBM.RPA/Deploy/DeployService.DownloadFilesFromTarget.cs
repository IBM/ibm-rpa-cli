using Joba.Pipeline;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA
{
    public partial class DeployService
    {
        class DownloadFilesFromTarget : DefaultPipelineMiddleware<StagingContext>
        {
            private readonly ILogger logger;

            public DownloadFilesFromTarget(ILogger logger) => this.logger = logger;

            protected override async Task Run(StagingContext context, CancellationToken cancellation)
            {
                if (context.Staging == null)
                    throw new InvalidOperationException("The 'staging' should have been created.");

                await DownloadFilesAsync(context.Client, context.Project, context.Staging, cancellation);
                await DownloadPackagesAsync(context.Client, context.Project, context.Staging, cancellation);
            }

            private async Task DownloadPackagesAsync(IRpaClient client, Project project, Environment staging, CancellationToken cancellation)
            {
                //TODO: we cannot use PackageManager here because it contains logic that does not fit the deploy.
                //because there are no 'packages' - they are just wal files
                //therefore, we need to download them locally from the 'to environment'
                //then overwrite them with the contents from the 'from environment'
                //then publish a new version of them to the 'to environment'.

                var downloader = new PackageAsWalDownloader(client, project, staging.Dependencies.Packages.Directory);
                downloader.Downloading += OnDownloading;
                downloader.Downloaded += OnDownloaded;

                logger.LogInformation("({Project}) downloading packages from {Remote}", project.Name, staging.Remote);
                var files = await downloader.DownloadIfExistsAsync(project.Dependencies.Packages.Select(p => p.Name).ToArray(), cancellation);

                void OnDownloading(object? sender, DownloadingEventArgs e) => logger.LogDebug("({Project}) downloading {Name} package", e.Project.Name, e.ResourceName);
                void OnDownloaded(object? sender, DownloadedEventArgs e) => logger.LogDebug("({Project}) {Total} downloaded", e.Project.Name, e.Total);
            }

            private async Task DownloadFilesAsync(IRpaClient client, Project project, Environment staging, CancellationToken cancellation)
            {
                //we need to download the 'to' wal files so we can overwrite them and publish them back, because of how the 'publish' API works.
                //publish API needs the latest version in order to publish a new wal content.
                //so we download the files but we will not use their contents, only their metadata.
                //download the wal file, overwrite its contents with 'from' environment file, then publish it back.
                // - also, we will update the package references with the new version before publishing it back.

                var pullService = new WalPullService(client, project, staging);
                pullService.Many.ShouldContinueOperation += (s, e) => e.Continue = true;
                pullService.Many.Pulling += OnPulling;
                pullService.Many.Pulled += OnPulled;

                logger.LogInformation("Downloading {Project} wal files from {Remote}", project.Name, staging.Remote);
                await pullService.Many.PullAsync(cancellation);

                void OnPulling(object? sender, PullingEventArgs e) => logger.LogDebug("({Project}) {Current}/{Total} downloading {ResourceName}", e.Project, e.Current, e.Total, e.ResourceName);
                void OnPulled(object? sender, PulledAllEventArgs e) => logger.LogDebug("({Project}) {Total} downloaded", e.Project.Name, e.Total);
            }

            class PackageAsWalDownloader
            {
                private readonly IRpaClient client;
                private readonly Project project;
                private readonly DirectoryInfo directory;

                public PackageAsWalDownloader(IRpaClient client, Project project, DirectoryInfo directory)
                {
                    this.client = client;
                    this.project = project;
                    this.directory = directory;
                }

                internal event EventHandler<DownloadingEventArgs>? Downloading;
                internal event EventHandler<DownloadedEventArgs>? Downloaded;

                internal async Task<IEnumerable<WalFile>> DownloadIfExistsAsync(string[] names, CancellationToken cancellation)
                {
                    if (!directory.Exists)
                        directory.Create();

                    var tasks = names.Select(n =>
                    {
                        Downloading?.Invoke(this, new DownloadingEventArgs { Project = project, ResourceName = n });
                        return client.Script.GetLatestVersionAsync(n, cancellation);
                    });
                    var scripts = (await Task.WhenAll(tasks)).Where(s => s != null).ToArray();
                    var wals = scripts.Select(s => WalFileFactory.Create(directory, s!)).ToArray();
                    Downloaded?.Invoke(this, new DownloadedEventArgs { Project = project, Total = wals.Length });
                    return wals;
                }
            }
        }
    }
}
