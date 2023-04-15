using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct EnvironmentSettingsFile
    {
        internal const string Extension = ".json";
        private readonly FileInfo file;

        internal EnvironmentSettingsFile(DirectoryInfo workingDirectory, string projectName, string alias)
            : this(BuildFileInfo(workingDirectory, projectName, alias), projectName, alias) { }

        private EnvironmentSettingsFile(FileInfo file, string projectName, string alias)
        {
            this.file = file;
            ProjectName = projectName;
            Alias = alias;
        }

        private static FileInfo BuildFileInfo(DirectoryInfo workingDirectory, string projectName, string alias) =>
           new(Path.Combine(workingDirectory.FullName, $"{projectName}.{alias}{Extension}"));

        internal string FullPath => file.FullName;
        internal bool Exists => file.Exists;
        internal string ProjectName { get; }
        internal string Alias { get; }

        internal static async Task<(EnvironmentSettingsFile, EnvironmentSettings?)> TryLoadAsync(
            DirectoryInfo workingDirectory, string projectName, string alias, CancellationToken cancellation)
        {
            var file = new EnvironmentSettingsFile(workingDirectory, projectName, alias);
            if (!file.Exists)
                return (file, null);

            using var stream = File.OpenRead(file.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<EnvironmentSettings>(stream, JsonSerializerOptionsFactory.SerializerOptions, cancellation)
                ?? throw new Exception($"Could not load environment '{file.Alias}' settings for project '{file.ProjectName} from '{file}'");

            return (file, settings);
        }

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }

    internal class EnvironmentSettings
    {
        /// <summary>
        /// NOTE: used by Json serializer.
        /// </summary>
        internal EnvironmentSettings() { }
        internal EnvironmentSettings(ILocalRepository<Parameter> parameters)
        {
            Parameters = parameters;
        }

        internal ILocalRepository<Parameter> Parameters { get; init; } = new ParameterRepository();
    }
}