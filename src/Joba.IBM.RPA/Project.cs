using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Joba.IBM.RPA
{
    public class Project
    {
        private  static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly DirectoryInfo workingDir;
        private readonly DirectoryInfo rpaDir;
        private readonly FileInfo projectFile;
        [JsonInclude]
        internal readonly IList<Environment> environments = new List<Environment>();
        private readonly IDictionary<string, Environment> environmentsMapping = new Dictionary<string, Environment>();

        [JsonConstructor]
        protected Project()
            : this("...loading...", System.Environment.CurrentDirectory) { }

        protected Project(string name, string workingDirPath)
        {
            Name = name;
            workingDir = new DirectoryInfo(workingDirPath);
            if (!workingDir.Exists)
                throw new DirectoryNotFoundException($"The directory '{workingDir.FullName}' does not exist");

            rpaDir = new DirectoryInfo(Constants.GetRpaDirectoryPath(workingDir.FullName));
            if (rpaDir.Exists)
                throw new Exception("A project has already been initilized in this folder");
            else
                rpaDir.CreateHidden();

            projectFile = new FileInfo(Constants.GetProjectFilePath(workingDir.FullName));
        }

        public string Name { get; init; }
        public ProjectSettings Settings { get; init; } = new ProjectSettings();
        [JsonIgnore]
        public Environment? CurrentEnvironment { get; private set; }

        public Environment AddEnvironment(string name, Region region, Account account, Session session)
        {
            var env = new Environment(name, region, account, session);
            environments.Add(env);
            environmentsMapping.Add(name, env);
            return env;
        }

        public void SwitchTo(string envName)
        {
            if (!environmentsMapping.ContainsKey(envName))
                throw new Exception($"Could not switch to {envName} because the environment does not exist");

            CurrentEnvironment = environmentsMapping[envName];
        }

        //public WalFile Get(string shortName)
        //{
        //    if (!shortName.EndsWith(WalFile.Extension))
        //        shortName = $"{shortName}{WalFile.Extension}";

        //    var file = new FileInfo(Path.Combine(workingDir.FullName, shortName));
        //    if (!file.Exists)
        //        throw new FileNotFoundException($"Wal file '{shortName}' does not exist", file.FullName);

        //    return WalFile.Read(file);
        //}

        public async Task SaveAsync(CancellationToken cancellation)
        {
            using var stream = File.OpenWrite(projectFile.FullName);
            await JsonSerializer.SerializeAsync(stream, this, SerializerOptions, cancellation);
        }

        public static Project Create(string name) => new(name, System.Environment.CurrentDirectory);

        public static async Task<Project> LoadAsync(CancellationToken cancellation)
        {
            var filePath = Constants.GetProjectFilePath(System.Environment.CurrentDirectory);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Could not load the project in the current directory because it does not exist", filePath);

            using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<Project>(stream, SerializerOptions, cancellation)
                ?? throw new Exception("Could not load project");
        }
    }

    public class Environment
    {
        [JsonInclude]
        internal readonly Configuration configuration;

        [JsonConstructor]
        protected Environment() { }

        public Environment(string name, Region region, Account account, Session session)
        {
            Name = name;
            configuration = new Configuration
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

        internal class Configuration
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

    public class ProjectSettings
    {
        public bool OverwriteOnFetch { get; private set; }

        public void AlwaysOverwriteOnFetch()
        {
            OverwriteOnFetch = true;
        }
    }
}