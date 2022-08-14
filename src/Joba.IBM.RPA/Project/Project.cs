using System.Diagnostics;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class Project
    {
        public static readonly string[] SupportedEnvironments = new string[] { Environment.Development, Environment.Testing, Environment.Production };
        private readonly DirectoryInfo workingDir;
        private readonly DirectoryInfo rpaDir;
        private readonly IDictionary<string, Environment> environmentsMapping = new Dictionary<string, Environment>();
        private readonly IList<Environment> environments = new List<Environment>();
        private readonly Lazy<ISession> lazySession;
        private string? name;
        private Environment? currentEnvironment;

        public Project(DirectoryInfo workingDir)
        {
            this.workingDir = workingDir;
            rpaDir = new DirectoryInfo(Constants.GetRpaDirectoryPath(workingDir.FullName));
            lazySession = new Lazy<ISession>(() =>
            {
                EnsureCurrentEnvironment();
                return new InternalSession(CurrentEnvironment);
            });
        }
        public Project(DirectoryInfo workingDir, string name)
            : this(workingDir)
        {
            this.name = name;
            if (rpaDir.Exists)
                throw new Exception($"A project is already configured in the '{workingDir.FullName}' folder");
            rpaDir.CreateHidden();
        }

        public string Name
        {
            get
            {
                EnsureLoaded();
#pragma warning disable CS8603 // Possible null reference return.
                return name;
#pragma warning restore CS8603 // Possible null reference return.
            }
        }
        public Environment CurrentEnvironment
        {
            get
            {
                EnsureLoaded();
                EnsureCurrentEnvironment();
#pragma warning disable CS8603 // Possible null reference return.
                return currentEnvironment;
#pragma warning restore CS8603 // Possible null reference return.
            }
        }
        public IList<Environment> Environments => environments;
        public ISession Session => lazySession.Value;

        public static async Task<Project> LoadFromCurrentDirectoryAsync(CancellationToken cancellation)
        {
            var project = new Project(new DirectoryInfo(System.Environment.CurrentDirectory));
            await project.LoadAsync(cancellation);
            return project;
        }
        public static Project CreateFromCurrentDirectory(string name) => new(new DirectoryInfo(System.Environment.CurrentDirectory), name);

        public void ConfigureEnvironmentAndSwitch(string environmentName, Region region, Account account, Session session)
        {
            if (environmentsMapping.ContainsKey(environmentName))
                throw new Exception($"Environment '{environmentName}' already exists");

            var env = new Environment(new EnvironmentFile(rpaDir, name, environmentName), region, account, session);
            AddEnvironment(env);
            SwitchTo(env.Name);
        }

        public void SwitchTo(string envName)
        {
            if (!environmentsMapping.ContainsKey(envName))
                throw new Exception($"Could not switch to '{envName}' because the environment does not exist");

            currentEnvironment = environmentsMapping[envName];
            currentEnvironment.MarkAsCurrent();
        }

        public WalFile? GetFile(string fileName)
        {
            EnsureCurrentEnvironment();
            return CurrentEnvironment.GetFile(fileName);
        }

        public IEnumerable<WalFile> GetFiles()
        {
            EnsureCurrentEnvironment();
            return CurrentEnvironment.GetFiles();
        }

        public async Task<WalFile> CreateFileAsync(IScriptClient client, string fileName, CancellationToken cancellation)
        {
            EnsureCurrentEnvironment();
            var version = await client.GetLatestVersionAsync(fileName, cancellation);
            if (version == null)
                throw new Exception($"Could not find the latest version of {fileName}");

            return CurrentEnvironment.CreateFile(fileName, version);
        }

        public async Task SaveAsync(CancellationToken cancellation)
        {
            EnsureCurrentEnvironment();
            await CurrentEnvironment.SaveAsync(cancellation);
        }

        private async Task LoadAsync(CancellationToken cancellation)
        {
            if (!rpaDir.Exists)
                throw new Exception($"Could not load project because it does not exist in '{workingDir}'");

            var collection = EnvironmentFileCollection.CreateAndEnsureValid(rpaDir);

            string? currentEnvironmentName = default;
            foreach (var envFile in collection)
            {
                var environment = await Environment.LoadAsync(envFile, cancellation);
                AddEnvironment(environment);
                if (environment.IsCurrent)
                    currentEnvironmentName = envFile.EnvironmentName;
            }

            name = collection.ProjectName;
            SwitchTo(currentEnvironmentName ?? Environment.Development);
        }

        private void EnsureLoaded()
        {
            if (!environments.Any())
                throw new InvalidOperationException($"The project hasn't been loaded. Please make sure to either call '{nameof(LoadAsync)}' or '{nameof(ConfigureEnvironmentAndSwitch)}' first.");
        }

        private void EnsureCurrentEnvironment()
        {
            if (currentEnvironment == null)
                throw new InvalidOperationException($"No current environment is set. Please make sure to use {nameof(SwitchTo)} method first.");
        }

        private void AddEnvironment(Environment environment)
        {
            environments.Add(environment);
            environmentsMapping.Add(environment.Name, environment);
        }

        private string GetDebuggerDisplay() => $"{name} ({string.Join(",", environmentsMapping.Keys)})";

        class InternalSession : ISession
        {
            public InternalSession(Environment environment)
            {
                Region = new Uri(environment.Account.RegionUrl);
                Token = environment.Account.Token;
            }

            public Uri Region { get; }
            public string Token { get; }
        }
    }
}