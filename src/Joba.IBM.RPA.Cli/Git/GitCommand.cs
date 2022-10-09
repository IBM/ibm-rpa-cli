namespace Joba.IBM.RPA.Cli
{
    internal partial class GitCommand : Command
    {
        internal const string CommandName = "git";

        internal GitCommand() : base(CommandName, "Provides integration with git")
        {
            AddCommand(new ConfigureCommand());
            AddCommand(new DiffCommand() { IsHidden = true });
            AddCommand(new MergeCommand() { IsHidden = true });
        }
    }
}
