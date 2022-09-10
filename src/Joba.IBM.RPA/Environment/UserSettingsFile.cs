using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    struct UserSettingsFile
    {
        internal const string FileName = "settings.json";
        private readonly FileInfo file;

        internal UserSettingsFile(string projectName)
            : this(new FileInfo(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "rpa", projectName, FileName)))
        { }

        private UserSettingsFile(FileInfo file)
        {
            this.file = file;
        }

        internal string FullPath => file.FullName;
        internal bool Exists => file.Exists;
        internal string ProjectName => file.Directory?.Parent?.Name ?? throw new Exception($"The grandparent directory of '{file.FullName}' should exist");

        internal async Task SaveAsync(UserSettings userSettings, CancellationToken cancellation)
        {
            if (!file.Directory!.Exists)
                file.Directory.Create();
            using var stream = new FileStream(FullPath, FileMode.Create);
            await JsonSerializer.SerializeAsync(stream, userSettings, JsonSerializerOptionsFactory.SerializerOptions, cancellation);
        }

        internal static async Task<(UserSettingsFile, UserSettings)> LoadAsync(string projectName, CancellationToken cancellation)
        {
            var file = new UserSettingsFile(projectName);
            if (file.Exists)
            {
                using var stream = File.OpenRead(file.FullPath);
                var settings = await JsonSerializer.DeserializeAsync<UserSettings>(stream, JsonSerializerOptionsFactory.SerializerOptions, cancellation)
                    ?? throw new Exception($"Could not user settings for the project '{projectName}' from '{file}'");

                return (file, settings);
            }

            return (file, new UserSettings());
        }

        public override string ToString() => file.FullName;

        private string GetDebuggerDisplay() => $"[{ProjectName}] {ToString()}";
    }

    internal class UserSettings
    {
        internal IDictionary<string, Session> Sessions { get; init; } = new Dictionary<string, Session>();

        internal Session? TryGetSession(string alias)
        {
            if (Sessions.TryGetValue(alias, out var value))
                return value;

            return null;
        }

        internal void AddOrUpdateSession(string alias, Session session)
        {
            if (Sessions.ContainsKey(alias))
                Sessions[alias] = session;
            else
                Sessions.Add(alias, session);
        }
    }
}