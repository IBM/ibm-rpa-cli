using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class GitCommand
    {
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
                var configurator = new GitConfigurator(logger, new DirectoryInfo(System.Environment.CurrentDirectory));
                if (remove)
                    await configurator.RemoveAsync(cancellation);
                else
                    await configurator.ConfigureAsync(cancellation);
            }
        }
    }
}
