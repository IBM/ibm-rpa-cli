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

                if (project.Robots.Exists(name))
                    throw new InvalidOperationException($"Bot named '{name}' already exists.");

                var template = new TemplateFactory(project.WorkingDirectory, Assembly.GetExecutingAssembly());
                _ = await template.CreateAsync(name, templateName, cancellation);
                var settings = RobotSettingsFactory.Create(templateName);
                project.Robots.Add(name.WithoutExtension, settings);

                await project.SaveAsync(cancellation);
                logger.LogInformation("Bot '{Bot}' created successfully based on '{Template}' template", name, templateName);
            }
        }
    }
}