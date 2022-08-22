using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct ProjectFile
    {
        public const string Extension = ".rpa.json";
        private readonly FileInfo file;
        private readonly DirectoryInfo rpaDir;

        public ProjectFile(DirectoryInfo workingDir, string projectName)
            : this(new FileInfo(Path.Combine(workingDir.FullName, $"{projectName}{Extension}"))) { }

        private ProjectFile(FileInfo file)
        {
            this.file = file;
            rpaDir = new DirectoryInfo(Path.Combine(file.Directory!.FullName, ".rpa"));
        }

        public string FullPath => file.FullName;
        public string ProjectName => file.Name.Replace(Extension, null);
        public DirectoryInfo RpaDirectory => rpaDir;
        public DirectoryInfo WorkingDirectory => file.Directory ?? throw new Exception($"The file directory of '{file}' should exist");

        public async Task SaveAsync(ProjectSettings projectSettings, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, projectSettings, Options.SerializerOptions, cancellation);
        }

        public static async Task<(ProjectFile, ProjectSettings)> LoadAsync(DirectoryInfo workingDir, CancellationToken cancellation)
        {
            var file = Find(workingDir);
            if (!file.RpaDirectory.Exists)
                throw new Exception($"Could not load project because there is no '.rpa' directory found within '{file.WorkingDirectory}'");

            using var stream = File.OpenRead(file.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<ProjectSettings>(stream, Options.SerializerOptions, cancellation)
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
        [JsonConstructor]
        public ProjectSettings() { }

        public ProjectSettings(NamePattern pattern)
        {
            Dependencies.Parameters.Add(pattern);
        }

        public string? CurrentEnvironment { get; set; } = string.Empty;
        [JsonPropertyName("environments")]
        public Dictionary<string, string> AliasMapping { get; init; } = new Dictionary<string, string>();
        public ProjectDependencies Dependencies { get; init; } = new ProjectDependencies();
        //public ProjectFiles Files { get; init; } = new ProjectFiles();

        public void MapAlias(string alias, string directoryPath) => AliasMapping.Add(alias, directoryPath);
        public bool EnvironmentExists(string alias) => AliasMapping.ContainsKey(alias);
        public DirectoryInfo GetDirectory(string alias)
        {
            if (!EnvironmentExists(alias))
                throw new Exception($"The environment '{alias}' does not exist");

            return new DirectoryInfo(AliasMapping[alias]);
        }

        //internal class ProjectFiles : IProjectFiles
        //{
        //    private List<NamePattern> files = new List<NamePattern>();
        //    public IEnumerable<NamePattern> Files { get => files; set => files = new List<NamePattern>(value); }

        //    internal void Add(string name) => files.Add(name);

        //    bool IProjectFiles.TryAdd(string name)
        //    {
        //        throw new NotImplementedException(); //TODO
        //    }
        //}
    }
}