using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct EnvironmentDependenciesFile
    {
        internal const string Extension = ".json";
        private readonly FileInfo file;

        internal EnvironmentDependenciesFile(DirectoryInfo environmentDir, string projectName, string alias)
            : this(BuildFileInfo(environmentDir, projectName, alias), projectName, alias) { }

        private EnvironmentDependenciesFile(FileInfo file, string projectName, string alias)
        {
            this.file = file;
            ProjectName = projectName;
            Alias = alias;
        }

        internal string FullPath => file.FullName;
        internal bool Exists => file.Exists;
        internal DirectoryInfo Directory => file.Directory ?? throw new Exception("Directory should not be null");
        internal string ProjectName { get; }
        internal string Alias { get; }

        private static FileInfo BuildFileInfo(DirectoryInfo environmentDir, string projectName, string alias) =>
            new(Path.Combine(environmentDir.FullName, $"{projectName}.{alias}{Extension}"));

        internal async Task SaveAsync(EnvironmentDependencies dependencies, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            var serializerOptions = JsonSerializerOptionsFactory.CreateForEnvironmentDependencies(file.Directory!);
            await JsonSerializer.SerializeAsync(stream, dependencies, serializerOptions, cancellation);
        }

        internal EnvironmentDependenciesFile CopyTo(DirectoryInfo directory)
        {
            var copiedFile = file.CopyTo(Path.Combine(directory.FullName, file.Name), true);
            return new EnvironmentDependenciesFile(copiedFile, ProjectName, Alias);
        }

        internal EnvironmentDependenciesFile CopyAndRenameTo(DirectoryInfo directory, string alias)
        {
            var copiedFile = file.CopyTo(BuildFileInfo(directory, ProjectName, alias).FullName, true);
            return new EnvironmentDependenciesFile(copiedFile, ProjectName, alias);
        }

        internal static async Task<(EnvironmentDependenciesFile, EnvironmentDependencies?)> TryLoadAsync(
            DirectoryInfo environmentDir, string projectName, string alias, CancellationToken cancellation)
        {
            var file = new EnvironmentDependenciesFile(environmentDir, projectName, alias);
            if (!file.Exists)
                return (file, null);

            var dependencies = await LoadAsync(file, cancellation);
            return (file, dependencies);
        }

        internal static async Task<EnvironmentDependencies> LoadAsync(EnvironmentDependenciesFile file, CancellationToken cancellation)
        {
            using var stream = File.OpenRead(file.FullPath);
            var serializerOptions = JsonSerializerOptionsFactory.CreateForEnvironmentDependencies(file.Directory);
            return await JsonSerializer.DeserializeAsync<EnvironmentDependencies>(stream, serializerOptions, cancellation)
                ?? throw new Exception($"Could not load environment '{file.Alias}' project ('{file.ProjectName}') dependencies from '{file}'");
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }
}