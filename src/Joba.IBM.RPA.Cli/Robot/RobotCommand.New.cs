using Microsoft.Extensions.Logging;
using System.Reflection;

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
                var name = new Argument<string>("name", description: "The bot name.");
                var template = new Option<string>("--template", () => "unattended", "The template to use. Depending on the template, different configurations are allowed.")
                    .FromAmong("attended", "chatbot", "unattended", "package", "excel");
                var properties = new Option<IEnumerable<string>?>(new[] { "--property", "-p" }, $"A key-value pair property to specify specific configurations for each template.") { AllowMultipleArgumentsPerToken = true };

                AddArgument(name);
                AddOption(template);
                AddOption(properties);
                this.SetHandler(HandleAsync, name, template,
                    new PropertyOptionsBinder(properties),
                    Bind.FromLogger<RobotCommand>(),
                    Bind.FromServiceProvider<IProject>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(string name, string templateName, PropertyOptions properties, ILogger<RobotCommand> logger, IProject project, InvocationContext context)
            {
                var handler = new NewRobotHandler(logger, project);
                await handler.HandleAsync(name, templateName, properties, context.GetCancellationToken());
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

                internal async Task HandleAsync(string name, string templateName, PropertyOptions properties, CancellationToken cancellation)
                {
                    if (project.Robots.Exists(name))
                        throw new ProjectException($"Robot named '{name}' already exists.");

                    if (!File.Exists(Path.Combine(project.WorkingDirectory.FullName, new WalFileName(name))))
                    {
                        var template = new TemplateFactory(project.WorkingDirectory, Assembly.GetExecutingAssembly());
                        _ = await template.CreateAsync(new WalFileName(name), templateName, cancellation);
                    }
                    var settings = RobotSettingsFactory.Create(templateName, name, properties);
                    project.Robots.Add(name, settings);

                    await project.SaveAsync(cancellation);
                    logger.LogInformation("Bot '{Bot}' created successfully based on '{Template}' template", name, templateName);
                }
            }
        }
    }
}