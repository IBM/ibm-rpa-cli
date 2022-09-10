namespace Joba.IBM.RPA
{
    public class PackageSourceException : Exception
    {
        public PackageSourceException(string message) : base(message) { }
    }

    public class PackageNotFoundException : PackageException
    {
        public PackageNotFoundException(string packageName)
            : this(packageName, $"Could not find '{packageName}' package.") { }
        public PackageNotFoundException(string packageName, WalVersion version)
            : this(packageName, $"Could not find package '{packageName}' with version '{version}'.") { }
        public PackageNotFoundException(string packageName, string message) : base(packageName, message) { }
    }

    public class PackageSourceNotFoundException : PackageSourceException
    {
        public PackageSourceNotFoundException(string alias)
            : base($"Could not find package source '{alias}'.") => Alias = alias;
        public string Alias { get; }
    }
}
