using System.Diagnostics.CodeAnalysis;

namespace Joba.IBM.RPA
{
    public struct NamePattern : IComparable<NamePattern>
    {
        private readonly string pattern;
        private readonly string nameWithoutStar;
        private readonly bool endsWithStar;

        public NamePattern(string pattern)
        {
            this.pattern = pattern;
            endsWithStar = pattern.EndsWith("*");
            nameWithoutStar = pattern.TrimEnd('*');
        }

        /// <summary>
        /// Gets the name without the wildcard.
        /// </summary>
        public string Name => nameWithoutStar;
        public bool HasWildcard => endsWithStar;

        internal bool Matches(string name) => HasWildcard ? name.StartsWith(nameWithoutStar) : name == nameWithoutStar;

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            if (obj is NamePattern name)
                return name.pattern == pattern;

            return false;
        }

        public override int GetHashCode() => pattern.GetHashCode();

        public override string ToString() => pattern;

        int IComparable<NamePattern>.CompareTo(NamePattern other) => pattern.CompareTo(other.pattern);

        //public static implicit operator string(NamePattern pattern) => pattern.ToString();

        public static bool operator ==(NamePattern left, NamePattern right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NamePattern left, NamePattern right)
        {
            return !(left == right);
        }
    }
}
