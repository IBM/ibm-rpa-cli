using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class GitCommand
    {
        internal static readonly string GitDiffCommandLine = $"{RpaCommand.CommandName} {GitCommand.CommandName} {GitCommand.DiffCommand.CommandName} \"$1\"";
        internal static readonly string GitMergeCommandLine = $"{RpaCommand.CommandName} {GitCommand.CommandName} {GitCommand.MergeCommand.CommandName} %O %A %B %P -v";

        internal class ConfigureCommand : Command
        {
            public ConfigureCommand() : base("config", "Configures git to understand .wal files.")
            {
                var remove = new Option<bool>("--remove", "Removes the configuration to integrate git to rpa");
                AddOption(remove);

                this.SetHandler(HandleAsync, remove,
                    Bind.FromLogger<GitCommand>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(bool remove, ILogger<GitCommand> logger, InvocationContext context)
            {
                if (!Directory.Exists(".git"))
                    throw new Exception("Git is not initialized in this directory");

                var cancellation = context.GetCancellationToken();
                var cliFile = new FileInfo(System.Environment.ProcessPath!);
                var configurator = new GitConfigurator(logger, new DirectoryInfo(System.Environment.CurrentDirectory),
                    cliFile, GitDiffCommandLine, GitMergeCommandLine);

                if (remove)
                    await configurator.RemoveAsync(cancellation);
                else
                    await configurator.ConfigureAsync(cancellation);
            }
        }
    }
}
