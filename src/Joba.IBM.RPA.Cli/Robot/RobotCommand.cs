using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class RobotCommand : Command
    {
        public const string CommandName = "bot";
        public RobotCommand() : base(CommandName, "Configures the bots that belong to the project.")
        {
            AddCommand(new NewBotCommand());
            AddCommand(new BuildBotCommand());
        }

        internal class BuildBotCommand : Command
        {
            public const string CommandName = "build";
            public BuildBotCommand() : base(CommandName, "Builds bots.")
            {
                var name = new Argument<WalFileName>("name", arg => new WalFileName(arg.Tokens[0].Value), description: "The bot name.");
                var directory = new Option<DirectoryInfo?>("--output", "Specifies the output directory.");

                AddArgument(name);
                this.SetHandler(HandleAsync, name, directory,
                    Bind.FromLogger<RobotCommand>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(WalFileName name, DirectoryInfo? outputDirectory, ILogger<RobotCommand> logger, Project project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();

                if (project.Robots.Exists(name))
                    throw new InvalidOperationException($"Bot named '{name}' does not exist.");

                var result = await project.BuildAsync(name, outputDirectory ?? project.RpaDirectory, cancellation);
                logger.LogInformation("Building '{name}' into '{output}' took {time}", result.Wals[0].Info.Name, result.Wals[0].Info.FullName, result.Time);
            }
        }
    }
}