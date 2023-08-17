using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class ProjectCommand
    {
        internal class NewProjectCommand : Command
        {
            public NewProjectCommand() : base("new", "Creates a RPA project in the current directory")
            {
                var name = new Argument<string>("name", "The project name.");
                var description = new Option<string?>("--description", "The project description.");
                description.AddAlias("-d");

                AddArgument(name);
                AddOption(description);
                this.SetHandler(HandleAsync, name, description,
                    Bind.FromLogger<NewProjectCommand>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, string? description, ILogger<NewProjectCommand> logger, InvocationContext context)
            {
                var handler = new NewProjectHandler(logger);
                await handler.HandleAsync(new DirectoryInfo(System.Environment.CurrentDirectory), name, description, context.GetCancellationToken());
            }
        }

        internal class NewProjectHandler
        {
            private readonly ILogger logger;

            public NewProjectHandler(ILogger logger)
            {
                this.logger = logger;
            }

            internal async Task HandleAsync(DirectoryInfo workingDir, string name, string? description, CancellationToken cancellation)
            {
                var project = ProjectFactory.Create(workingDir, name, description);
                await project.SaveAsync(cancellation);

                logger.LogInformation("Project '{ProjectName}' has been initialized. Use '{RpaCommandName} {BuildCommand}' to build the project. Or '{RpaCommandName} {RobotCommand} {NewBotCommand} [name]' to create bots",
                    project.Name, RpaCommand.CommandName, BuildCommand.CommandName, RpaCommand.CommandName, RobotCommand.CommandName, RobotCommand.NewBotCommand.CommandName);
            }
        }
    }
}