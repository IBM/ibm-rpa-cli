using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct EnvironmentFile
    {
        internal const string Extension = ".json";
        private readonly FileInfo file;

        internal EnvironmentFile(DirectoryInfo rpaDirectory, string projectName, string alias)
            : this(new FileInfo(Path.Combine(rpaDirectory.FullName, $"{projectName}.{alias}{Extension}")), projectName, alias) { }

        private EnvironmentFile(FileInfo file, string projectName, string alias)
        {
            this.file = file;
            ProjectName = projectName;
            Alias = alias;
        }

        internal string FullPath => file.FullName;
        internal bool Exists => file.Exists;
        internal string ProjectName { get; }
        internal string Alias { get; }

        internal async Task SaveAsync(EnvironmentSettings settings, CancellationToken cancellation)
        {
            using var stream = new FileStream(FullPath, FileMode.Create); //TODO: on all saves, make a copy ~name-of-file, save->replace->delete-copy
            await JsonSerializer.SerializeAsync(stream, settings, JsonSerializerOptionsFactory.SerializerOptions, cancellation);
        }

        internal static async Task<(EnvironmentFile, EnvironmentSettings)> LoadAsync(
            DirectoryInfo rpaDir, string projectName, string alias, CancellationToken cancellation)
        {
            var file = new EnvironmentFile(rpaDir, projectName, alias);
            using var stream = File.OpenRead(file.FullPath);
            var settings = await JsonSerializer.DeserializeAsync<EnvironmentSettings>(stream, JsonSerializerOptionsFactory.SerializerOptions, cancellation)
                ?? throw new Exception($"Could not load environment '{alias}' from '{file}'");

            return (file, settings);
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}]({Alias}) {ToString()}";
    }

    public class RemoteSettings
    {
        public required string Name { get; init; }
        public required Uri Address { get; init; }
        public required int TenantCode { get; init; }
        public required string TenantName { get; init; }
        public required string PersonName { get; init; }
        public required string UserName { get; init; }

        internal static RemoteSettings Create(Region region, Session session)
        {
            return new RemoteSettings
            {
                Name = region.Name,
                Address = region.ApiAddress,
                PersonName = session.PersonName,
                TenantCode = session.TenantCode,
                TenantName = session.TenantName,
                UserName = session.UserName
            };
        }
    }

    internal class EnvironmentSettings
    {
        internal required bool IsDefault { get; init; }
        internal required RemoteSettings Remote { get; init; }
    }
}