namespace Joba.IBM.RPA.Cli
{
    [RequiresProject]
    internal partial class EnvironmentCommand : Command
    {
        public static readonly string CommandName = "env";
        internal EnvironmentCommand() : base(CommandName, "Manages environments")
        {
            AddCommand(new NewEnvironmentCommand());
        }
    }
}