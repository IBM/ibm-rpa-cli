namespace Joba.IBM.RPA
{
    internal class ProjectDependencies : IProjectDependencies
    {
        //private List<NamePattern> parameters = new List<NamePattern>();

        public IParameterDependencies Parameters { get; init; } = new ParameterDependencies();
        //public IEnumerable<NamePattern> Parameters { get => parameters; set => parameters = new List<NamePattern>(value); }

        //void IProjectDependencies.AddParameter(string parameter)
        //{
        //    parameters.Add(parameter);
        //    parameters.Sort();
        //}

        //void IProjectDependencies.SetParameters(string[] parameters)
        //{
        //    this.parameters.Clear();
        //    this.parameters.AddRange(parameters.OrderBy(p => p));
        //}
    }

    internal class ParameterDependencies : IParameterDependencies
    {
        private readonly List<NamePattern> parameters = new();
        private readonly List<NamePattern> withWildcards = new();
        private readonly List<string> withoutWildcards = new();

        public ParameterDependencies() { }

        public ParameterDependencies(IEnumerable<NamePattern> parameters)
        {
            this.parameters = new List<NamePattern>(parameters);
            (withWildcards, withoutWildcards) = Split(parameters);
        }

        private static (List<NamePattern>, List<string>) Split(IEnumerable<NamePattern> parameters)
        {
            var withWildcards = new List<NamePattern>();
            var withoutWildcards = new List<string>();
            foreach (var parameter in parameters)
            {
                if (parameter.HasWildcard)
                    withWildcards.Add(parameter);
                else
                    withoutWildcards.Add(parameter.Name);
            }

            return (withWildcards, withoutWildcards);
        }

        IEnumerable<NamePattern> IParameterDependencies.GetWildcards() => withWildcards;
        IEnumerable<string> IParameterDependencies.GetFixed() => withoutWildcards;
        bool IParameterDependencies.Contains(string name) => parameters.Any(p => p.Matches(name));

        void IParameterDependencies.Add(NamePattern pattern)
        {
            if (pattern.HasWildcard)
            {
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
        IParameterDependencies Parameters { get; }
        //bool ContainsParameter(string parameter);
        //IEnumerable<NamePattern> Parameters { get; }
        //void SetParameters(string[] parameters);
        //void AddParameter(string parameter);
    }

    public interface IParameterDependencies : IEnumerable<NamePattern>
    {
        void Add(NamePattern pattern);
        bool Contains(string name);
        IEnumerable<NamePattern> GetWildcards();
        IEnumerable<string> GetFixed();
    }
}
