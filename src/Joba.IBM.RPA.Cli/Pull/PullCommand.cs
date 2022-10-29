using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Xml.Linq;

namespace Joba.IBM.RPA.Cli
{
    [RequiresProject]
    internal partial class PullCommand : Command
    {
        public PullCommand() : base("pull", "Pulls all the project files")
        {
            var name = new Argument<string>("name", "The asset name. To pull several at once, use '*' at the end, e.g 'MyParam*'.");
            var environmentName = new Option<string>("--env", "The alias of the environment to pull parameters from.") { Arity = ArgumentArity.ExactlyOne };
            var assetType = new Option<string?>("--type", "The type of the asset to pull. If not provided, assets from all types will be pulled.") { Arity = ArgumentArity.ZeroOrOne }
                .FromAmong("wal", "parameter");

            AddArgument(name);
            AddOption(environmentName);
            AddOption(assetType);

            this.SetHandler(HandleAsync, name, environmentName, assetType,
                Bind.FromLogger<PullCommand>(),
                Bind.FromServiceProvider<IRpaClientFactory>(),
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string name, string environmentName, string? assetType, ILogger<PullCommand> logger, IRpaClientFactory clientFactory, Project project, InvocationContext context)
        {
            var handler = new PullHandler(logger, clientFactory, context.Console, project);
            await handler.HandleAsync(new NamePattern(name), environmentName, assetType, context.GetCancellationToken());
        }
    }
}