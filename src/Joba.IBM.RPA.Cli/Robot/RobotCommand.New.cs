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
            public NewBotCommand() : base(CommandName, "Scaffolds wal files based on the bot type.")
            {
                var name = new Argument<WalFileName>("name", arg => new WalFileName(arg.Tokens[0].Value), description: "The bot name.");
                var type = new Option<string>("--type", () => "unattended", "The type of the bot. Depending on the type, different configurations are allowed.")
                    .FromAmong("attended", "chatbot", "unattended");

                AddArgument(name);
                AddOption(type);
                this.SetHandler(HandleAsync, name, type,
                    Bind.FromLogger<RobotCommand>(),
                    Bind.FromServiceProvider<Project>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(WalFileName name, string type, ILogger<RobotCommand> logger, Project project, InvocationContext context)
            {
                var cancellation = context.GetCancellationToken();

                if (project.Robots.Exists(name))
                    throw new InvalidOperationException($"Bot named '{name}' already exists.");

                var template = new TemplateFactory(project.WorkingDirectory, Assembly.GetExecutingAssembly());
                _ = await template.CreateAsync(name, type, cancellation);
                var settings = RobotSettingsFactory.Create(type);
                project.Robots.Add(name, settings);

                await project.SaveAsync(cancellation);
            }
        }
    }
}