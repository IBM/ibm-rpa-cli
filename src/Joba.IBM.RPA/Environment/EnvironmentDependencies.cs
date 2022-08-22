using System.Diagnostics;

namespace Joba.IBM.RPA
{
    internal class EnvironmentDependencies : IEnvironmentDependencies
    {
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        IEnumerable<Parameter> IEnvironmentDependencies.Parameters => Parameters.Select(p => new Parameter(p.Key, p.Value)).ToArray();

        void IEnvironmentDependencies.AddOrUpdate(Parameter[] parameters)
        {
            foreach (var parameter in parameters)
            {
                if (Parameters.ContainsKey(parameter.Name))
                    Parameters[parameter.Name] = parameter.Value;
                else
                    Parameters.Add(parameter.Name, parameter.Value);
            }
        }

        Parameter? IEnvironmentDependencies.GetParameter(string name) =>
            Parameters.TryGetValue(name, out var value) ? new Parameter(name, value) : null;
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct DependenciesFile
    {
        internal const string Extension = ".json";
        private readonly FileInfo file;

        internal DependenciesFile(DirectoryInfo environmentDir, string projectName, string alias)
            : this(new FileInfo(Path.Combine(environmentDir.FullName, $"{projectName}.{alias}{Extension}")), projectName, alias) { }

        private DependenciesFile(FileInfo file, string projectName, string alias)
        {
            this.file = file;
            ProjectName = projectName;
            Alias = alias;
        }

        internal string FullPath => file.FullName;
        internal bool Exists => file.Exists;
        internal string ProjectName { get; }
        internal string Alias { get; }

        internal async Task SaveAsync(EnvironmentDependencies dependencies, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, dependencies, Options.SerializerOptions, cancellation);
        }

        internal static async Task<(DependenciesFile, EnvironmentDependencies?)> LoadAsync(
            DirectoryInfo environmentDir, string projectName, string alias, CancellationToken cancellation)
        {
            var file = new DependenciesFile(environmentDir, projectName, alias);
            if (!file.Exists)
                return (file, null);

            using var stream = File.OpenRead(file.FullPath);
            var dependencies = await JsonSerializer.DeserializeAsync<EnvironmentDependencies>(stream, Options.SerializerOptions, cancellation)
                ?? throw new Exception($"Could not load environment '{alias}' project ('{projectName}') dependencies from '{file}'");

            return (file, dependencies);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }

    public interface IEnvironmentDependencies
    {
        IEnumerable<Parameter> Parameters { get; }
        void AddOrUpdate(params Parameter[] parameters);
        Parameter? GetParameter(string name);
    }
}