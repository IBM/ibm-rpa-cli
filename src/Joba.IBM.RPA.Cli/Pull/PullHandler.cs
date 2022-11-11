using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal class PullHandler
    {
        private readonly ILogger logger;
        private readonly IRpaClientFactory clientFactory;
        private readonly IConsole console;
        private readonly IProject project;

        public PullHandler(ILogger logger, IRpaClientFactory clientFactory, IConsole console, IProject project)
        {
            this.logger = logger;
            this.clientFactory = clientFactory;
            this.console = console;
            this.project = project;
        }

        internal Task HandleAsync(NamePattern pattern, string environmentName, string? assetType, CancellationToken cancellation)
        {
            IPullHandler handler;
            if (assetType == null)
                handler = new PullAllHandler(logger, clientFactory, console, project);
            else if (assetType == "wal")
                handler = new WalPullHandler(logger, clientFactory, console, project);
            else if (assetType == "parameter")
                handler = new ParameterPullHandler(logger, clientFactory, console, project);
            else
                throw new NotSupportedException($"Asset type {assetType} is not supported.");

            return handler.HandleAsync(pattern, environmentName, cancellation);
        }
    }

    internal interface IPullHandler
    {
        Task HandleAsync(NamePattern pattern, string environmentName, CancellationToken cancellation);
    }
}