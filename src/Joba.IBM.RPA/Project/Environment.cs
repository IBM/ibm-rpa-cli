using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Joba.IBM.RPA
{
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
}