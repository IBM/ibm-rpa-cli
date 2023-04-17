namespace Joba.IBM.RPA
{
    public class PackageException : Exception
    {
        public PackageException(string packageName, string message) : base(message) => PackageName = packageName;
        public string PackageName { get; }
    }

    public class PackageNotFoundException : PackageException
    {
        public PackageNotFoundException(string packageName)
            : this(packageName, $"Could not find '{packageName}' package.") { }
        public PackageNotFoundException(string packageName, WalVersion version)
            : this(packageName, $"Could not find package '{packageName}' with version '{version}'.") { }
        public PackageNotFoundException(string packageName, string message) : base(packageName, message) { }
    }

    public class PackageAlreadyInstalledException : PackageException
    {
        public PackageAlreadyInstalledException(string packageName, WalVersion version)
            : base(packageName, $"The package '{packageName}' is already installed with the following version: {version}.") { }
    }
}
