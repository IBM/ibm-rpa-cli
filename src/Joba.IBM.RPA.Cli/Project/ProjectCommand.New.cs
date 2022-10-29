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

                AddArgument(name);
                this.SetHandler(HandleAsync, name,
                    Bind.FromLogger<NewProjectCommand>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, ILogger<NewProjectCommand> logger, InvocationContext context)
            {
                var handler = new NewProjectHandler(logger);
                await handler.HandleAsync(new DirectoryInfo(System.Environment.CurrentDirectory), name, context.GetCancellationToken());
            }
        }

        internal class NewProjectHandler
        {
            private readonly ILogger logger;

            public NewProjectHandler(ILogger logger)
            {
                this.logger = logger;
            }

            internal async Task HandleAsync(DirectoryInfo workingDir, string name, CancellationToken cancellation)
            {
                var project = ProjectFactory.Create(logger, workingDir, name);
                await project.SaveAsync(cancellation);

                //TODO: create "build" command
                //logger.LogInformation("Project '{ProjectName}' has been initialized. Use '{RpaCommandName} {BuildCommand} <bot-name>' to build scripts into bots.",
                //    project.Name, RpaCommand.CommandName, BuildCommand.CommandName);
                logger.LogInformation("Project '{ProjectName}' has been initialized.", project.Name);
            }
        }
    }
}