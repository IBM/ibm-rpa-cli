using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct ProjectFile
    {
        internal const string Extension = ".rpa.json";
        private readonly FileInfo file;
        private readonly DirectoryInfo rpaDir;

        internal ProjectFile(DirectoryInfo workingDir, string projectName)
            : this(new FileInfo(Path.Combine(workingDir.FullName, $"{projectName}{Extension}"))) { }

        private ProjectFile(FileInfo file)
        {
            this.file = file;
            rpaDir = new DirectoryInfo(Path.Combine(file.Directory!.FullName, ".rpa"));
        }

        internal string FullPath => file.FullName;
        internal string ProjectName => file.Name.Replace(Extension, null);
        internal DirectoryInfo RpaDirectory => rpaDir;
        internal DirectoryInfo WorkingDirectory => file.Directory ?? throw new Exception($"The file directory of '{file}' should exist");

        internal async Task SaveAsync(ProjectSettings projectSettings, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, projectSettings, JsonSerializerOptionsFactory.SerializerOptions, cancellation);
        }

        internal static async Task<(ProjectFile, ProjectSettings)> LoadAsync(DirectoryInfo workingDir, CancellationToken cancellation)
        {
            var file = Find(workingDir);
            if (!file.RpaDirectory.Exists)
                throw new Exception($"Could not load project because there is no '.rpa' directory found within '{file.WorkingDirectory}'");

            using var stream = File.OpenRead(file.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<ProjectSettings>(stream, JsonSerializerOptionsFactory.SerializerOptions, cancellation)
                ?? throw new Exception($"Could not load project '{file.ProjectName}' from '{file}'");

            return (file, settings);
        }

        private static ProjectFile Find(DirectoryInfo workingDir)
        {
            var files = workingDir.GetFiles($"*{Extension}", SearchOption.TopDirectoryOnly);
            if (files.Length > 1)
                throw new Exception($"Cannot load the project because the '{workingDir.FullName}' directory should only contain one '{Extension}' file. " +
                    $"Files found: {string.Join(", ", files.Select(f => f.Name))}");

            if (files.Length == 0)
                throw new Exception($"Cannot load the project because no '{Extension}' file was found in the '{workingDir.FullName}' directory");

            return new ProjectFile(files[0]);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }

    internal class ProjectSettings
    {
        public ProjectSettings()
        {

        }
        internal string? CurrentEnvironment { get; set; } = string.Empty;
        [JsonPropertyName("environments")]
        internal Dictionary<string, string> AliasMapping { get; init; } = new Dictionary<string, string>();
        internal INames Files { get; init; } = new NamePatternList();
        internal ProjectDependencies Dependencies { get; init; } = new ProjectDependencies();

        internal void Configure(NamePattern pattern)
        {
            Files.Add(pattern);
            Dependencies.Configure(pattern);
        }

        internal void MapAlias(string alias, string directoryPath) => AliasMapping.Add(alias, directoryPath);
        internal bool EnvironmentExists(string alias) => AliasMapping.ContainsKey(alias);
        internal DirectoryInfo GetDirectory(string alias)
        {
            if (!EnvironmentExists(alias))
                throw new Exception($"The environment '{alias}' does not exist");

            return new DirectoryInfo(AliasMapping[alias]);
        }
    }
}