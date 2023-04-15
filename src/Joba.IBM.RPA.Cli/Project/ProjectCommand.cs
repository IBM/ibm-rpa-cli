namespace Joba.IBM.RPA.Cli
{
    internal partial class ProjectCommand : Command
    {
        public ProjectCommand() : base("project", "Manages RPA projects")
        {
            AddCommand(new NewProjectCommand());
        }
    }
}