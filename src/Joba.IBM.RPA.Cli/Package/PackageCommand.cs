namespace Joba.IBM.RPA.Cli
{
    internal partial class PackageCommand : Command
    {
        public const string CommandName = "package";
        public PackageCommand() : base(CommandName, "Manages package dependencies")
        {
            AddCommand(new PackageSourceCommand());
            AddCommand(new InstallPackageCommand());
            AddCommand(new UpdatePackageCommand());
            AddCommand(new RestorePackageCommand());
            AddCommand(new UninstallPackageCommand());
        }
    }
}