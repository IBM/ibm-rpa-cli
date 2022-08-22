using System;
using System.Diagnostics;
using static Joba.IBM.RPA.ProjectSettings;

namespace Joba.IBM.RPA
{
    public class Project
    {
        private readonly ProjectFile projectFile;
        private readonly ProjectSettings projectSettings;

        internal Project(ProjectFile projectFile, NamePattern pattern)
            : this(projectFile, new ProjectSettings(pattern)) { }

        internal Project(ProjectFile projectFile, ProjectSettings projectSettings)
        {
            this.projectFile = projectFile;
            this.projectSettings = projectSettings;
        }

        public string Name => projectFile.ProjectName;
        public IProjectDependencies Dependencies => projectSettings.Dependencies;
        //public IProjectFiles Files => projectSettings.Files;

        public async Task SaveAsync(CancellationToken cancellation) =>
            await projectFile.SaveAsync(projectSettings, cancellation);

        public Environment ConfigureEnvironmentAndSwitch(string alias, Region region, Session session)
        {
            var environment = EnvironmentFactory.Create(projectFile, alias, region, session);
            projectSettings.MapAlias(alias, Path.GetRelativePath(projectFile.WorkingDirectory.FullName, environment.Directory.FullName));
            SwitchTo(environment.Alias);

            return environment;
        }

        public async Task<Environment?> GetCurrentEnvironmentAsync(CancellationToken cancellation) =>
            await EnvironmentFactory.LoadAsync(projectFile.RpaDirectory, projectFile, projectSettings, cancellation);

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
    }

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

    //public interface IProjectFiles
    //{
    //    IEnumerable<string> Files { get; }
    //    bool TryAdd(string name);
    //}

    public interface IProjectDependencies
    {
        IParameterDependencies Parameters { get; }
        //bool ContainsParameter(string parameter);
        //IEnumerable<NamePattern> Parameters { get; }
        //void SetParameters(string[] parameters);
        //void AddParameter(string parameter);
    }

    //[JsonDerivedType(typeof(ParameterDependencies))]
    //[JsonPolymorphic]
    public interface IParameterDependencies : IEnumerable<NamePattern>
    {
        void Add(NamePattern pattern);
        bool Contains(string name);
        IEnumerable<NamePattern> GetWildcards();
        IEnumerable<string> GetFixed();
    }
}