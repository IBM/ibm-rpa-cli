namespace Joba.IBM.RPA
{
    public class PackageException : Exception
    {
        public PackageException(string packageName, string message) : base(message) => PackageName = packageName;
        public string PackageName { get; }
    }

    public class PackageAlreadyInstalledException : PackageException
    {
        public PackageAlreadyInstalledException(string packageName, WalVersion version)
            : base(packageName, $"The package '{packageName}' is already installed with the following version: {version}.") { }
    }
}
