namespace Joba.IBM.RPA
{
    internal class ParameterRepository : ILocalRepository<Parameter>
    {
        private readonly IDictionary<string, Parameter> mappings;

        internal ParameterRepository()
            : this(new Dictionary<string, string>()) { }

        internal ParameterRepository(IDictionary<string, string> parameters)
        {
            mappings = parameters.Select(p => new Parameter(p.Key, p.Value)).ToDictionary(k => k.Name, v => v);
        }

        void ILocalRepository<Parameter>.AddOrUpdate(params Parameter[] parameters)
        {
            foreach (var parameter in parameters)
            {
                if (mappings.ContainsKey(parameter.Name))
                    mappings[parameter.Name] = parameter;
                else
                    mappings.Add(parameter.Name, parameter);
            }
        }

        void ILocalRepository<Parameter>.Update(Parameter parameter)
        {
            if (mappings.ContainsKey(parameter.Name))
                mappings[parameter.Name] = parameter;
            else
                throw new Exception($"Could not update the parameter '{parameter.Name}' because it does not exist.");
        }

        Parameter? ILocalRepository<Parameter>.Get(string name) =>
            mappings.TryGetValue(name, out var value) ? value : null;
        void ILocalRepository<Parameter>.Remove(string name) => mappings.Remove(name);
        void ILocalRepository<Parameter>.Clear() => mappings.Clear();

        public IEnumerator<Parameter> GetEnumerator() => mappings.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}