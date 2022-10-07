namespace Joba.IBM.RPA.Cli
{
    internal partial class GitCommand : Command
    {
        internal const string CommandName = "git";

        internal GitCommand() : base(CommandName, "Provides integration with git")
        {
            //TODO: git merge support: https://git-scm.com/docs/git-mergetool
            //using VSCode: https://code.visualstudio.com/docs/editor/command-line#_core-cli-options
            AddCommand(new ConfigureCommand());
            AddCommand(new DiffCommand());
        }
    }
}
