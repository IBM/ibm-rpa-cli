using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class ProjectCommand
    {
        internal class NewProjectCommand : Command
        {
            public NewProjectCommand() : base("new", "Creates a RPA project in the current directory")
            {
                var name = new Argument<string>("name", "The project name. This will be used as the pattern to fetch wal files if '--pattern' is not specified.");
                var pattern = new Option<string?>("--pattern", "Specifies the pattern to fetch files, e.g 'Assistant*'");
                var serverUrl = new Option<string?>("--url", $"The server domain url. You can specify '{ServerAddress.DefaultOptionName}' to use {ServerAddress.DefaultUrl}");
                var environmentName = new Option<string?>("--env", "Specifies the first environment to set up after the project is created.");

                AddArgument(name);
                AddOption(pattern);
                AddOption(serverUrl);
                AddOption(environmentName);
                this.SetHandler(HandleAsync, name, pattern, serverUrl, environmentName,
                    Bind.FromLogger<NewProjectCommand>(),
                    Bind.FromServiceProvider<IRpaClientFactory>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, string? pattern, string? serverUrl, string? environmentName,
                ILogger<NewProjectCommand> logger, IRpaClientFactory clientFactory, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var project = ProjectFactory.CreateFromCurrentDirectory(name, new NamePattern(pattern ?? name + "*"));
                await project.SaveAsync(cancellation);

                if (string.IsNullOrEmpty(environmentName))
                    logger.LogInformation("Project '{ProjectName}' has been initialized. Use '{RpaCommandName} {EnvironmentCommandName} {AddEnvironmentCommandName}' to add environments.",
                        project.Name, RpaCommand.CommandName, EnvironmentCommand.CommandName, EnvironmentCommand.AddEnvironmentCommand.CommandName);
                else
                {
                    var command = new EnvironmentCommand.AddEnvironmentCommand();
                    await command.HandleAsync(new RemoteOptions(environmentName, new ServerAddress(serverUrl)), logger, clientFactory, project, context);
                }
            }
        }
    }
}