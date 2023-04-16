using Microsoft.Extensions.Logging;
using System.Reflection;
using static Joba.IBM.RPA.Cli.ProjectCommand;

namespace Joba.IBM.RPA.Cli
{
    internal partial class RobotCommand
    {
        [RequiresProject]
        internal class NewBotCommand : Command
        {
            public const string CommandName = "new";
            public NewBotCommand() : base(CommandName, "Scaffolds wal files based on the templates.")
            {
                var name = new Argument<WalFileName>("name", arg => new WalFileName(arg.Tokens[0].Value), description: "The bot name.");
                var template = new Option<string>("--template", () => "unattended", "The template to use. Depending on the template, different configurations are allowed.")
                    .FromAmong("attended", "chatbot", "unattended", "excel");

                AddArgument(name);
                AddOption(template);
                this.SetHandler(HandleAsync, name, template,
                    Bind.FromLogger<RobotCommand>(),
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(WalFileName name, string templateName, ILogger<RobotCommand> logger, IProject project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();

                var handler = new NewRobotHandler(logger, project);
                await handler.HandleAsync(name, templateName, context.GetCancellationToken());
            }

            internal class NewRobotHandler
            {
                private readonly ILogger logger;
                private readonly IProject project;

                public NewRobotHandler(ILogger logger, IProject project)
                {
                    this.logger = logger;
                    this.project = project;
                }

                internal async Task HandleAsync(WalFileName name, string templateName, CancellationToken cancellation)
                {
                    if (project.Robots.Exists(name.WithoutExtension))
                        throw new ProjectException($"Robot named '{name}' already exists.");

                    var template = new TemplateFactory(project.WorkingDirectory, Assembly.GetExecutingAssembly());
                    _ = await template.CreateAsync(name, templateName, cancellation);
                    var settings = RobotSettingsFactory.Create(templateName, name.WithoutExtension);
                    project.Robots.Add(name.WithoutExtension, settings);

                    await project.SaveAsync(cancellation);
                    logger.LogInformation("Bot '{Bot}' created successfully based on '{Template}' template", name, templateName);
                }
            }
        }
    }
}