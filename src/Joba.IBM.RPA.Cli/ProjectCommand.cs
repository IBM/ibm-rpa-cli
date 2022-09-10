using System;

namespace Joba.IBM.RPA.Cli
{
    internal class ProjectCommand : Command
    {
        public ProjectCommand() : base("project", "Manages RPA projects")
        {
            AddCommand(new NewProjectCommand());
            AddCommand(new InitializeProjectCommand());
        }

        internal class NewProjectCommand : Command
        {
            public NewProjectCommand() : base("new", "Creates a RPA project in the current directory")
            {
                var name = new Argument<string>("name", "The project name. This will be used as the pattern to fetch wal files if '--pattern' is not specified.");
                var pattern = new Option<string?>("--pattern", "Specifies the pattern to fetch files, e.g 'Assistant*'");
                var environmentName = new Option<string?>("--env", "Specifies the first environment to set up after the project is created.");

                AddArgument(name);
                AddOption(pattern);
                AddOption(environmentName);
                this.SetHandler(HandleAsync, name, pattern, environmentName, Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, string? pattern, string? environmentName, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var project = ProjectFactory.CreateFromCurrentDirectory(name, new NamePattern(pattern ?? name + "*"));
                await project.SaveAsync(cancellation);

                if (string.IsNullOrEmpty(environmentName))
                    ExtendedConsole.WriteLine($"Project {project.Name:blue} has been initialized. " +
                        $"Use {RpaCommand.CommandName:blue} {EnvironmentCommand.CommandName:blue} to add environments.");
                else
                {
                    var command = new EnvironmentCommand();
                    await command.HandleAsync(new EnvironmentCommand.Options(environmentName), project, cancellation);
                }
            }
        }

        [RequiresProject]
        internal class InitializeProjectCommand : Command
        {
            public InitializeProjectCommand() : base("init", "Initializes the already created project in the current directory")
            {
                this.SetHandler(HandleAsync,
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(Project project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();
                var environment = await project.GetCurrentEnvironmentAsync(cancellation);
                if (environment == null)
                {
                    ExtendedConsole.WriteLine($"The project has been initialized, but no environment has been configured yet. " +
                        $"Use {RpaCommand.CommandName:blue} {EnvironmentCommand.CommandName:blue} to configure one.");
                }
                else
                {
                    var client = RpaClientFactory.CreateFromEnvironment(environment);
                    var sessionEnsurer = new EnvironmentSessionEnsurer(client, environment);
                    var session = await sessionEnsurer.EnsureAsync(cancellation);
                    ExtendedConsole.WriteLine($"Hi {session.PersonName:blue}, the project has been initialized.");
                }
            }
        }
    }
}