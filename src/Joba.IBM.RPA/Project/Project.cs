using System;
using System.Diagnostics;

namespace Joba.IBM.RPA
{
    public class Project
    {
        private readonly ProjectFile projectFile;
        private readonly ProjectSettings projectSettings;

        internal Project(ProjectFile projectFile)
            : this(projectFile, new ProjectSettings()) { }

        internal Project(ProjectFile projectFile, ProjectSettings projectSettings)
        {
            this.projectFile = projectFile;
            this.projectSettings = projectSettings;
        }

        public string Name => projectFile.ProjectName;
        public IProjectDependencies Dependencies => projectSettings.Dependencies;

        public async Task SaveAsync(CancellationToken cancellation)
        {
            await projectFile.SaveAsync(projectSettings, cancellation);
        }

        public Environment ConfigureEnvironmentAndSwitch(string alias, Region region, Session session)
        {
            var environment = EnvironmentFactory.Create(projectFile, alias, region, session);
            projectSettings.MapAlias(alias, Path.GetRelativePath(projectFile.WorkingDirectory.FullName, environment.Directory.FullName));
            SwitchTo(environment.Alias);

            return environment;
        }

        public async Task<Environment?> GetCurrentEnvironmentAsync(CancellationToken cancellation)
        {
            return await EnvironmentFactory.LoadAsync(projectFile.RpaDirectory, projectFile, projectSettings, cancellation);
        }

        public bool SwitchTo(string alias)
        {
            if (!projectSettings.EnvironmentExists(alias))
                throw new Exception($"The environment '{alias}' does not exist");

            if (projectSettings.CurrentEnvironment == null ||
                !projectSettings.CurrentEnvironment.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
            {
                projectSettings.CurrentEnvironment = alias;
                return true;
            }

            return false;
        }
    }

    internal class ProjectSettings
    {
        public string? CurrentEnvironment { get; set; } = string.Empty;
        [JsonPropertyName("environments")]
        public Dictionary<string, string> AliasMapping { get; init; } = new Dictionary<string, string>();
        public ProjectDependencies Dependencies { get; init; } = new ProjectDependencies();

        public void MapAlias(string alias, string directoryPath) => AliasMapping.Add(alias, directoryPath);
        public bool EnvironmentExists(string alias) => AliasMapping.ContainsKey(alias);
        public DirectoryInfo GetDirectory(string alias)
        {
            if (!EnvironmentExists(alias))
                throw new Exception($"The environment '{alias}' does not exist");

            return new DirectoryInfo(AliasMapping[alias]);
        }

        internal class ProjectDependencies : IProjectDependencies
        {
            private List<string> parameters = new List<string>();

            public IEnumerable<string> Parameters { get => parameters; set => parameters = new List<string>(value); }

            void IProjectDependencies.AddParameter(string parameter)
            {
                parameters.Add(parameter);
                parameters.Sort();
            }

            void IProjectDependencies.SetParameters(string[] parameters)
            {
                this.parameters.Clear();
                this.parameters.AddRange(parameters.OrderBy(p => p));
            }
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct ProjectFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver()
        };
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
            await JsonSerializer.SerializeAsync(stream, projectSettings, SerializerOptions, cancellation);
        }

        public static async Task<(ProjectFile, ProjectSettings)> LoadAsync(DirectoryInfo workingDir, CancellationToken cancellation)
        {
            var file = Find(workingDir);
            if (!file.RpaDirectory.Exists)
                throw new Exception($"Could not load project because there is no '.rpa' directory found within '{file.WorkingDirectory}'");

            using var stream = File.OpenRead(file.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<ProjectSettings>(stream, SerializerOptions, cancellation)
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

    public interface IProjectDependencies
    {
        IEnumerable<string> Parameters { get; }
        void SetParameters(string[] parameters);
        void AddParameter(string parameter);
    }
}