namespace Joba.IBM.RPA
{
    public class Environment
    {
        private readonly ISessionManager session;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;

        internal Environment(string alias, RemoteSettings remote, UserSettingsFile userFile, UserSettings userSettings)
        {
            this.userFile = userFile;
            this.userSettings = userSettings ?? new UserSettings();
            Alias = alias;
            Remote = remote;
            session = new SessionManager(alias, this.userFile, this.userSettings, remote);
        }

        public string Alias { get; }
        public RemoteSettings Remote { get; }
        public ISessionManager Session => session;

        internal async Task<(EnvironmentSettingsFile, EnvironmentSettings)> LoadSettingsAsync(IProject project, CancellationToken cancellation)
        {
            var (file, settings) = await EnvironmentSettingsFile.TryLoadAsync(project.WorkingDirectory, project.Name, Alias, cancellation);
            return (file, settings ?? new EnvironmentSettings(project.Parameters));
        }

        public override string ToString() => $"{Alias} ({Remote.TenantName}), [{Remote.Region}]({Remote.Address})";
    }

    public interface IEnvironments
    {
        Environment this[string alias] { get; }
    }

    internal class Environments : IEnvironments
    {
        private readonly ProjectSettings projectSettings;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;

        internal Environments(ProjectSettings projectSettings, UserSettingsFile userFile, UserSettings userSettings)
        {
            this.projectSettings = projectSettings;
            this.userFile = userFile;
            this.userSettings = userSettings;
        }

        Environment IEnvironments.this[string alias] =>
            projectSettings.Environments.TryGetValue(alias, out var value) ?
                new(alias, value, userFile, userSettings) :
                throw EnvironmentException.NotConfigured(alias);
    }
}