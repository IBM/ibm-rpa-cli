using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct EnvironmentDependenciesFile
    {
        internal const string Extension = ".json";
        private readonly FileInfo file;

        internal EnvironmentDependenciesFile(DirectoryInfo environmentDir, string projectName, string alias)
            : this(new FileInfo(Path.Combine(environmentDir.FullName, $"{projectName}.{alias}{Extension}")), projectName, alias) { }

        private EnvironmentDependenciesFile(FileInfo file, string projectName, string alias)
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
            var serializerOptions = JsonSerializerOptionsFactory.CreateForEnvironmentDependencies(file.Directory!);
            await JsonSerializer.SerializeAsync(stream, dependencies, serializerOptions, cancellation);
        }

        internal static async Task<(EnvironmentDependenciesFile, EnvironmentDependencies?)> TryLoadAsync(
            DirectoryInfo environmentDir, string projectName, string alias, CancellationToken cancellation)
        {
            var file = new EnvironmentDependenciesFile(environmentDir, projectName, alias);
            if (!file.Exists)
                return (file, null);

            using var stream = File.OpenRead(file.FullPath);
            var serializerOptions = JsonSerializerOptionsFactory.CreateForEnvironmentDependencies(environmentDir);
            var dependencies = await JsonSerializer.DeserializeAsync<EnvironmentDependencies>(stream, serializerOptions, cancellation)
                ?? throw new Exception($"Could not load environment '{alias}' project ('{projectName}') dependencies from '{file}'");

            return (file, dependencies);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }
}