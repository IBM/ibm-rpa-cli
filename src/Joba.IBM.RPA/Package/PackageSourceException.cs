namespace Joba.IBM.RPA
{
    public class PackageSourceException : Exception
    {
        public PackageSourceException(string message) : base(message) { }
    }

    public class PackageSourceNotFoundException : PackageSourceException
    {
        public PackageSourceNotFoundException(string alias)
            : base($"Could not find package source '{alias}'.") => Alias = alias;
        public string Alias { get; }
    }
}
