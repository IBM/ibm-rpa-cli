using System;
using System.Diagnostics;

namespace Joba.IBM.RPA
{
    public class Project
    {
        private readonly DirectoryInfo workingDir;
        private readonly ProjectFile projectFile;
        private readonly ProjectSettings projectSettings;

        internal Project(DirectoryInfo rpaDir, string name)
            : this(new ProjectFile(rpaDir, name), new ProjectSettings()) { }

        internal Project(ProjectFile projectFile, ProjectSettings projectSettings)
        {
            this.projectFile = projectFile;
            this.projectSettings = projectSettings;
            workingDir = projectFile.RpaDirectory.Parent ??
                throw new InvalidOperationException($"The '{nameof(projectFile.RpaDirectory)}' directory should have a parent (the working directory)");
        }

        public string Name => projectFile.ProjectName;

        public async Task SaveAsync(CancellationToken cancellation)
        {
            await projectFile.SaveAsync(projectSettings, cancellation);
        }

        public Environment ConfigureEnvironmentAndSwitch(string alias, Region region, Session session)
        {
            var environment = EnvironmentFactory.Create(workingDir, projectFile, alias, region, session);
            projectSettings.MapAlias(alias, Path.GetRelativePath(workingDir.FullName, environment.Directory.FullName));
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

        public void MapAlias(string alias, string directoryPath) => AliasMapping.Add(alias, directoryPath);
        public bool EnvironmentExists(string alias) => AliasMapping.ContainsKey(alias);
        public DirectoryInfo GetDirectory(string alias)
        {
            if (!EnvironmentExists(alias))
                throw new Exception($"The environment '{alias}' does not exist");

            return new DirectoryInfo(AliasMapping[alias]);
        }
    }

    internal class UserSettings
    {
        public string? Token { get; set; }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct UserSettingsFile
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new IncludeInternalMembersJsonTypeInfoResolver()
        };
        public static readonly string FileName = "settings.json";
        private readonly FileInfo file;

        public UserSettingsFile(string projectName, string alias)
            : this(new FileInfo(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "rpa", projectName, alias, FileName)))
        { }

        private UserSettingsFile(FileInfo file)
        {
            this.file = file;
        }

        public string FullPath => file.FullName;
        public bool Exists => file.Exists;
        public string ProjectName => file.Directory?.Parent?.Name ?? throw new Exception($"The grandparent directory of '{file.FullName}' should exist");
        public string Alias => file.Directory?.Name ?? throw new Exception($"The parent directory of '{file.FullName}' should exist");

        public async Task SaveAsync(UserSettings userSettings, CancellationToken cancellation)
        {
            if (!file.Directory!.Exists)
                file.Directory.Create();
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, userSettings, SerializerOptions, cancellation);
        }

        public static async Task<(UserSettingsFile, UserSettings?)> LoadAsync(string projectName, string alias, CancellationToken cancellation)
        {
            var file = new UserSettingsFile(projectName, alias);
            if (file.Exists)
            {
                using var stream = File.OpenRead(file.FullPath);
                var settings = await JsonSerializer.DeserializeAsync<UserSettings>(stream, SerializerOptions, cancellation)
                    ?? throw new Exception($"Could not user settings for the project '{projectName}' from '{file}'");

                return (file, settings);
            }

            return (file, null);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}]({Alias}) {ToString()}";
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
        public static readonly string Extension = ".prpa";
        private readonly FileInfo file;

        public ProjectFile(DirectoryInfo rpaDir, string projectName)
            : this(new FileInfo(Path.Combine(rpaDir.FullName, $"{projectName}{Extension}"))) { }

        private ProjectFile(FileInfo file)
        {
            this.file = file;
        }

        public string FullPath => file.FullName;
        public string ProjectName => Path.GetFileNameWithoutExtension(file.Name);
        public DirectoryInfo RpaDirectory => file.Directory ?? throw new Exception($"The file directory of '{file}' should exist");

        public async Task SaveAsync(ProjectSettings projectSettings, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, projectSettings, SerializerOptions, cancellation);
        }

        public static async Task<(ProjectFile, ProjectSettings)> LoadAsync(DirectoryInfo rpaDir, CancellationToken cancellation)
        {
            var file = Find(rpaDir);
            using var stream = File.OpenRead(file.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<ProjectSettings>(stream, SerializerOptions, cancellation)
                ?? throw new Exception($"Could not load project '{file.ProjectName}' from '{file}'");

            return (file, settings);
        }

        private static ProjectFile Find(DirectoryInfo rpaDir)
        {
            var files = rpaDir.GetFiles($"*{Extension}", SearchOption.TopDirectoryOnly);
            if (files.Length > 1)
                throw new Exception($"Cannot load the project because the '{rpaDir.FullName}' directory should only contain one '{Extension}' file. " +
                    $"Files found: {string.Join(", ", files.Select(f => f.Name))}");

            if (files.Length == 0)
                throw new Exception($"Cannot load the project because no '{Extension}' file was found in the '{rpaDir.FullName}' directory");

            return new ProjectFile(files[0]);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }
}