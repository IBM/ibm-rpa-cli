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

        internal bool Exists => file.Exists;
        internal string FullPath => file.FullName;
        internal string ProjectName => file.Name.Replace(Extension, null);
        internal DirectoryInfo RpaDirectory => rpaDir;
        internal DirectoryInfo WorkingDirectory => file.Directory ?? throw new Exception($"The file directory of '{file}' should exist");

        internal async Task SaveAsync(ProjectSettings projectSettings, CancellationToken cancellation)
        {
            var serializer = JsonSerializerOptionsFactory.CreateForProject(rpaDir.Parent!);
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, projectSettings, serializer, cancellation);
        }

        internal static async Task<(ProjectFile?, ProjectSettings?)> TryLoadAsync(DirectoryInfo workingDir, CancellationToken cancellation)
        {
            var file = Find(workingDir);
            if (file == null)
                return (null, null);
            if (!file.Value.Exists)
                return (file, null);

            var serializer = JsonSerializerOptionsFactory.CreateForProject(workingDir);
            using var stream = File.OpenRead(file.Value.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<ProjectSettings>(stream, serializer, cancellation)
                ?? throw new Exception($"Could not load project '{file.Value.ProjectName}' from '{file}'");

            return (file, settings);
        }

        private static ProjectFile? Find(DirectoryInfo workingDir)
        {
            var files = workingDir.GetFiles($"*{Extension}", SearchOption.TopDirectoryOnly);
            if (files.Length > 1)
                throw new Exception($"Cannot load the project because the '{workingDir.FullName}' directory should only contain one '{Extension}' file. " +
                    $"Files found: {string.Join(", ", files.Select(f => f.Name))}");

            if (files.Length == 0)
                return null;

            return new ProjectFile(files[0]);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }

    internal class ProjectSettings
    {
        internal ProjectSettings(DirectoryInfo workingDirectory) => Packages = new PackageReferences(workingDirectory);

        internal Dictionary<string, RemoteSettings> Environments { get; init; } = new Dictionary<string, RemoteSettings>();

        internal string Description { get; init; } = string.Empty;
        /// <summary>
        /// NOTE: polymorphic serialization/deserialization: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism?pivots=dotnet-7-0
        /// </summary>
        internal Robots Robots { get; init; } = new Robots();
        internal PackageReferences Packages { get; init; }
        internal ParameterRepository Parameters { get; init; } = new ParameterRepository();

        internal void MapEnvironment(string alias, RemoteSettings remote) => Environments.Add(alias, remote);
        internal bool EnvironmentExists(string alias) => Environments.ContainsKey(alias);
    }

    public class RemoteSettings
    {
        [JsonPropertyName("code")]
        public required int TenantCode { get; init; }
        [JsonPropertyName("name")]
        public required string TenantName { get; init; }
        public required string Region { get; init; }
        public required Uri Address { get; init; }

        internal static RemoteSettings Create(Region region, CreatedSession session)
        {
            return new RemoteSettings
            {
                Region = region.Name,
                Address = region.ApiAddress,
                TenantCode = session.TenantCode,
                TenantName = session.TenantName
            };
        }

        public string ToString(string alias) => $"{alias} ({TenantName}), [{Region}]({Address})";
        public override string ToString() => $"{TenantName}, [{Region}]({Address})";
    }
}