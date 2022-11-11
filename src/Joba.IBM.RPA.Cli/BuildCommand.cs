using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal class BuildCommand : Command
    {
        public const string CommandName = "build";
        public BuildCommand() : base(CommandName, "Builds projects and bots.")
        {
            var name = new Option<WalFileName?>("--name", arg => new WalFileName(arg.Tokens[0].Value), description: "Optionally the name of a particular bot.") { Arity = ArgumentArity.ZeroOrOne };
            var output = new Option<DirectoryInfo?>("--output", "Specifies the output directory.") { Arity = ArgumentArity.ZeroOrOne };

            AddOption(name);
            AddOption(output);
            this.SetHandler(HandleAsync, name, output,
                Bind.FromLogger<RobotCommand>(),
                Bind.FromServiceProvider<IProject>(),
                Bind.FromServiceProvider<ICompiler>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(WalFileName? name, DirectoryInfo? outputDirectory, ILogger<RobotCommand> logger, IProject project, ICompiler compiler, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();

            if (name.HasValue && !project.Robots.Exists(name))
                throw new InvalidOperationException($"Bot named '{name}' does not exist.");

            Robot? robot = name.HasValue ? project.Robots[name.Value] : null;
            var arguments = new BuildArguments(project, robot, null, outputDirectory ?? new DirectoryInfo(Path.Combine(project.RpaDirectory.FullName, "build")));
            var result = await compiler.BuildAsync(arguments, cancellation);
            if (result.Success)
                logger.LogInformation("Build took {Time} and succeed", result.GetTimeAsString());
            else
                logger.LogError(result.Error!, "Build took {Time} and failed with {ErrorMessage}", result.GetTimeAsString(), result.Error!.Message);
        }
    }
}