using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Joba.IBM.RPA
{
    /* Ideas
     * - project.json
     *   - holds active environment
     *   - ?? what other things ??
     * - project.dev.json -> holds settings & environment data
     * - project.test.json
     * - project.prod.json
     */

    class EnvironmentFileCollection : IEnumerable<EnvironmentFile>
    {
        private readonly IEnumerable<EnvironmentFile> files;

        private EnvironmentFileCollection(DirectoryInfo rpaDir)
        {
            files = rpaDir
                .EnumerateFiles($"*{EnvironmentFile.FileExtension}", SearchOption.TopDirectoryOnly)
                .Select(f => new EnvironmentFile(f))
                .Where(f => f.IsParsed)
                .ToList();

            EnsureValid(rpaDir);
        }

        public static IEnumerable<EnvironmentFile> CreateAndEnsureValid(DirectoryInfo rpaDir) =>
            new EnvironmentFileCollection(rpaDir).ToArray();

        private void EnsureValid(DirectoryInfo rpaDir)
        {
            if (!files.Any())
                throw new Exception($"Could not load project because there are not environment files within '{rpaDir.FullName}'");

            EnsureSameProject();
            EnsureDifferentEnvironments();
        }

        private void EnsureDifferentEnvironments()
        {
            var differentEnvironments = files.GroupBy(f => f.EnvironmentName).All(g => g.Count() == files.Count());
            if (!differentEnvironments)
                throw new Exception(
                    $"Could not load the project because all environments should be different." +
                    $"Loaded files:{System.Environment.NewLine}" +
                    string.Join(System.Environment.NewLine, files.Select(f => f.File.Name)));
        }

        private void EnsureSameProject()
        {
            var sameFileNames = files.GroupBy(f => f.ProjectName).All(g => g.Count() == 1);
            if (!sameFileNames)
                throw new Exception(
                    $"Could not load the project because all environment files should have the same name." +
                    $"Loaded files:{System.Environment.NewLine}" +
                    string.Join(System.Environment.NewLine, files.Select(f => f.File.Name)));
        }

        public IEnumerator<EnvironmentFile> GetEnumerator()
        {
            return files.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)files).GetEnumerator();
        }
    }

    [DebuggerDisplay("{" + nameof(File) + "}")]
    struct EnvironmentFile
    {
        public static readonly string FileExtension = ".json";
        private static readonly string FileNameGroup = "fileName";
        private static readonly string EnvironmentGroup = "environment";
        private static readonly Regex EnvironmentFileExpression = new($@"(?<{FileNameGroup}>^[^\.]+)\.(?<{EnvironmentGroup}>[^\.]+)\{FileExtension}");

        public EnvironmentFile(FileInfo file)
        {
            File = file;
            var match = EnvironmentFileExpression.Match(file.Name);
            IsParsed = match.Success;
            if (IsParsed)
            {
                ProjectName = match.Groups[FileNameGroup].Value;
                EnvironmentName = match.Groups[EnvironmentGroup].Value;
            }
        }

        public EnvironmentFile(DirectoryInfo rpaDir, string projectName, string environmentName)
            : this(new FileInfo(Path.Combine(rpaDir.FullName, $"{projectName}.{environmentName}{FileExtension}"))) { }

        public FileInfo File { get; }
        public string? ProjectName { get; } = null;
        public string? EnvironmentName { get; } = null;
        internal bool IsParsed { get; }

        public override string ToString() => File.FullName;
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class Project
    {
        public static readonly string[] SupportedEnvironments = new string[] { Environment.Development, Environment.Testing, Environment.Production };
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new IncludeInternalPropertyJsonTypeInfoResolver()
        };

        private string? name;
        private Environment? currentEnvironment;
        private readonly DirectoryInfo rpaDir;
        private readonly IDictionary<string, Environment> environmentsMapping = new Dictionary<string, Environment>();
        private readonly IList<Environment> environments = new List<Environment>();

        public Project(DirectoryInfo workingDir) => rpaDir = new DirectoryInfo(Constants.GetRpaDirectoryPath(workingDir.FullName));
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
                using var stream = File.OpenRead(envFile.File.FullName);
                var environment = await JsonSerializer.DeserializeAsync<Environment>(stream, SerializerOptions, cancellation)
                    ?? throw new Exception($"Could not load environment '{envFile.EnvironmentName}' from '{envFile.File.Name}'");

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

        public void ConfigureEnvironmentAndSwitch(string name, Region region, Account account, Session session)
        {
            if (environmentsMapping.ContainsKey(name))
                throw new Exception($"Environment '{name}' already exists");

            var env = new Environment(name, region, account, session);
            AddEnvironment(env);
            SwitchTo(env.Name);
        }

        public void SwitchTo(string envName)
        {
            if (!environmentsMapping.ContainsKey(envName))
                throw new Exception($"Could not switch to '{envName}' because the environment does not exist");

            currentEnvironment = environmentsMapping[envName];
        }

        public async Task SaveAsync(CancellationToken cancellation)
        {
            EnsureCurrentEnvironment();
            var file = new EnvironmentFile(rpaDir, Name, CurrentEnvironment.Name);
            using var stream = File.OpenWrite(file.File.FullName);
            await JsonSerializer.SerializeAsync(stream, CurrentEnvironment, SerializerOptions, cancellation);
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

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class Environment
    {
        public static readonly string Development = "dev";
        public static readonly string Testing = "test";
        public static readonly string Production = "prod";

        [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal Environment() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public Environment(string name, Region region, Account account, Session session)
        {
            Name = name;
            Account = new AccountConfiguration
            {
                RegionName = region.Name,
                RegionUrl = region.ApiUrl.ToString(),
                PersonName = session.PersonName,
                TenantCode = account.TenantCode,
                TenantName = session.TenantName,
                UserName = account.UserName,
                UserPassword = account.Password
            };
        }

        public string Name { get; init; }
        public bool IsCurrent { get; init; }
        public EnvironmentSettings Settings { get; init; } = new EnvironmentSettings();
        internal AccountConfiguration Account { get; init; } = new AccountConfiguration();

        private string GetDebuggerDisplay() => $"{Name} ({Account.RegionName}), Tenant = {Account.TenantName}, User = {Account.UserName}";

        internal class AccountConfiguration
        {
            public string RegionName { get; set; }
            public string RegionUrl { get; set; }
            public int TenantCode { get; set; }
            public string TenantName { get; set; }
            public string PersonName { get; set; }
            public string UserName { get; set; }
            public string UserPassword { get; set; }
        }
    }

    public class EnvironmentSettings
    {
        public bool OverwriteOnFetch { get; private set; }

        public void AlwaysOverwriteOnFetch()
        {
            OverwriteOnFetch = true;
        }
    }
}