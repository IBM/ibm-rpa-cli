using System;

namespace Joba.IBM.RPA
{
    public class Environment
    {
        private readonly DirectoryInfo envDir;
        private readonly ISessionManager session;
        private readonly LocalWalRepository repository;
        private readonly UserSettingsFile userFile;
        private readonly UserSettings userSettings;
        private readonly EnvironmentDependenciesFile dependenciesFile;
        private EnvironmentDependencies? dependencies;

        internal Environment(string alias, DirectoryInfo envDir, RemoteSettings remote, UserSettingsFile userFile,
            UserSettings userSettings, EnvironmentDependenciesFile dependenciesFile)
            : this(alias, envDir, remote, userFile, userSettings,
                  dependenciesFile, null)
        { }

        internal Environment(string alias, DirectoryInfo envDir, RemoteSettings remote, UserSettingsFile userFile, UserSettings userSettings,
            EnvironmentDependenciesFile dependenciesFile, EnvironmentDependencies? dependencies)
        {
            Alias = alias;
            Remote = remote;
            this.envDir = envDir;
            this.userFile = userFile;
            this.userSettings = userSettings ?? new UserSettings();
            this.dependenciesFile = dependenciesFile;
            this.dependencies = dependencies;
            repository = new LocalWalRepository(envDir);
            session = new SessionManager(alias, this.userFile, this.userSettings, remote);
        }

        public string Alias { get; }
        public RemoteSettings Remote { get; }
        public DirectoryInfo Directory => envDir;
        public ISessionManager Session => session;
        public ILocalRepository Files => repository;
        public IEnvironmentDependencies Dependencies => dependencies ??= new EnvironmentDependencies(envDir);

        public async Task SaveAsync(CancellationToken cancellation)
        {
            if (!envDir.Exists)
                envDir.Create();

            await userFile.SaveAsync(userSettings, cancellation);
            if (dependencies != null)
                await dependenciesFile.SaveAsync(dependencies, cancellation);
        }

        /// <summary>
        /// Uses this instance to create a staging environment merging configurations from the <paramref name="to"/> environment and this instance.
        /// </summary>
        /// <param name="directory">The staging directory.</param>
        /// <param name="to">The environment that the staging will be based on.</param>
        /// <param name="cancellation">A cancellation token.</param>
        /// <returns>An environment loaded from <paramref name="directory"/> with merged configurations.</returns>
        internal async Task<Environment> StageAsync(DirectoryInfo directory, Environment to, CancellationToken cancellation)
        {
            //try to find <env>/<project>.<to>.json instead of <env>/<project>.<alias>.json to use.
            //for example: from <dev> to <prod> of <Assistant> project
            //the user will create Assistant.prod.json in the <dev> environment, so when they deploy, that file should be used instead of Assistant.dev.json
            //TODO: we should actually 'merge' those files, because <prod> may override only a few configurations - the rest of configurations should be taken from <dev>.

            var toDependenciesFile = new EnvironmentDependenciesFile(envDir, userFile.ProjectName, to.Alias);
            if (toDependenciesFile.Exists)
                toDependenciesFile = toDependenciesFile.CopyTo(directory);
            else
                toDependenciesFile = dependenciesFile.CopyAndRenameTo(directory, to.Alias);

            var toDependencies = await EnvironmentDependenciesFile.LoadAsync(toDependenciesFile, cancellation);
            return new Environment(to.Alias, directory, to.Remote, userFile, userSettings, toDependenciesFile, toDependencies);
        }

        public override string ToString() => $"{Alias} ({Remote.TenantName}), [{Remote.Region}]({Remote.Address})";
    }
}