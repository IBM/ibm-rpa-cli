namespace Joba.IBM.RPA
{
    public class PackageAlreadyInstalledException : Exception
    {
        public PackageAlreadyInstalledException(string name, WalVersion version)
            : base($"The package '{name}' is already installed with the following version: {version}.")
        {
            PackageName = name;
        }

        public string PackageName { get; }
    }

    public class PackageNotFoundException : Exception
    {
        public PackageNotFoundException(string name)
            : base($"Could not find '{name}' package to update.")
        {
            PackageName = name;
        }

        public string PackageName { get; }
    }
}
