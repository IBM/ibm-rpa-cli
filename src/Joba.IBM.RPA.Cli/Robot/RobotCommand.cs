namespace Joba.IBM.RPA.Cli
{
    internal partial class RobotCommand : Command
    {
        public const string CommandName = "bot";
        public RobotCommand() : base(CommandName, "Configures the bots that belong to the project.")
        {
            AddCommand(new NewBotCommand());
        }
    }
}