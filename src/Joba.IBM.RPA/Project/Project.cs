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
        private string? name;
        private Environment? currentEnvironment;

        public Project(DirectoryInfo workingDir)
        {
            this.workingDir = workingDir;
            rpaDir = new DirectoryInfo(Constants.GetRpaDirectoryPath(workingDir.FullName));
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
                if (string.IsNullOrEmpty(name))
                {
                    EnsureLoaded();
                    name = environments[0].Name;
                }

                return name;
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

        private async Task LoadAsync(CancellationToken cancellation)
        {
            var environmentFiles = EnvironmentFileCollection.CreateAndEnsureValid(rpaDir);

            string? currentEnvironmentName = default;
            foreach (var envFile in environmentFiles)
            {
                var environment = await Environment.LoadAsync(envFile, cancellation);
                AddEnvironment(environment);
                if (environment.IsCurrent)
                    currentEnvironmentName = envFile.EnvironmentName;
            }

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

        public void ConfigureEnvironmentAndSwitch(string environmentName, Region region, Account account, Session session)
        {
            if (environmentsMapping.ContainsKey(environmentName))
                throw new Exception($"Environment '{environmentName}' already exists");

            var env = new Environment(new EnvironmentFile(rpaDir, Name, environmentName), region, account, session);
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

        public async Task SaveAsync(CancellationToken cancellation)
        {
            EnsureCurrentEnvironment();
            await CurrentEnvironment.SaveAsync(cancellation);
        }

        public static async Task<Project> LoadFromCurrentDirectoryAsync(CancellationToken cancellation)
        {
            var project = new Project(new DirectoryInfo(System.Environment.CurrentDirectory));
            await project.LoadAsync(cancellation);
            return project;
        }
        public static Project CreateFromCurrentDirectory(string name) => new(new DirectoryInfo(System.Environment.CurrentDirectory), name);

        private string GetDebuggerDisplay() => $"{name} ({string.Join(",", environmentsMapping.Keys)})";
    }
}