namespace Joba.IBM.RPA
{
    internal class ProjectDependencies : IProjectDependencies
    {
        public INames Parameters { get; init; } = new NamePatternList();

        internal void Configure(NamePattern pattern)
        {
            Parameters.Add(pattern);
            //TODO: add for chat/credential/etc
        }
    }

    internal class NamePatternList : INames
    {
        private readonly List<NamePattern> parameters = new();
        private readonly List<NamePattern> withWildcards = new();
        private readonly List<string> withoutWildcards = new();

        internal NamePatternList() { }

        internal NamePatternList(IEnumerable<NamePattern> parameters)
        {
            this.parameters = new List<NamePattern>(parameters);
            (withWildcards, withoutWildcards) = parameters.Split();
        }

        IEnumerable<NamePattern> INames.GetWildcards() => withWildcards;
        IEnumerable<string> INames.GetFixed() => withoutWildcards;
        bool INames.IsTracked(string name) => parameters.Any(p => p.Matches(name));

        void INames.Add(NamePattern pattern)
        {
            if (pattern.HasWildcard)
            {
                if (withWildcards.Contains(pattern))
                    throw new Exception($"The pattern name '{pattern}' is already set.");
                //TODO: improve logic
                //has:      Assistant*
                //attempts: Assis*
                //          should add (and maybe remove the other)
                //has:      Assistant*
                //attempts: Assistant_*
                //          already has, no need to add
                //has:      Assistant_Test
                //attempts: Assistant*
                //          should add (and remove all 'hardcoded' assistant)
                parameters.Add(pattern);
                withWildcards.Add(pattern);
            }
            else
            {
                //should not add 'Assistant_Test' if there is 'Assistant*', because it's already covered.
                if (!parameters.Any(p => p.Matches(pattern.Name)))
                {
                    parameters.Add(pattern);
                    withoutWildcards.Add(pattern.Name);
                }
            }
        }

        IEnumerator<NamePattern> IEnumerable<NamePattern>.GetEnumerator() => parameters.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => parameters.GetEnumerator();
    }

    public interface IProjectDependencies
    {
        INames Parameters { get; }
    }

    public interface INames : IEnumerable<NamePattern>
    {
        void Add(NamePattern pattern);
        bool IsTracked(string name);
        IEnumerable<NamePattern> GetWildcards();
        IEnumerable<string> GetFixed();
    }
}
