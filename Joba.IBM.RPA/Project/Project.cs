using System.Text.Json;

namespace Joba.IBM.RPA
{
    class Project
    {
        private readonly DirectoryInfo workingDir;
        private readonly DirectoryInfo rpaDir;
        private readonly FileInfo projectFile;

        public Project(string workingDirPath) : this(new DirectoryInfo(workingDirPath)) { }

        public Project(DirectoryInfo workingDir)
        {
            if (!workingDir.Exists)
                throw new DirectoryNotFoundException($"The directory '{workingDir.FullName}' does not exist");

            this.workingDir = workingDir;
            rpaDir = new DirectoryInfo(Path.Combine(workingDir.FullName, ".rpa"));
            projectFile = new FileInfo(Path.Combine(rpaDir.FullName, "project.json"));
            Settings = new ProjectSettings();
        }

        public ProjectSettings Settings { get; private set; }

        public async Task CreateAsync(CancellationToken cancellation)
        {
            if (rpaDir.Exists)
                throw new Exception("A project has already been initilized in this folder");

            rpaDir.CreateHidden();
            await SaveAsync(cancellation);
        }

        public IEnumerable<WalFile> EnumerableWalFiles()
        {
            var files = workingDir.EnumerateFiles($"*{WalFile.Extension}", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
                yield return WalFile.Read(file);
        }

        public WalFile Get(string shortName)
        {
            if (!shortName.EndsWith(WalFile.Extension))
                shortName = $"{shortName}{WalFile.Extension}";

            var file = new FileInfo(Path.Combine(workingDir.FullName, shortName));
            if (!file.Exists)
                throw new FileNotFoundException($"Wal file '{shortName}' does not exist", file.FullName);

            return WalFile.Read(file);
        }

        public async Task SaveAsync(CancellationToken cancellation)
        {
            using var stream = File.OpenWrite(projectFile.FullName);
            await JsonSerializer.SerializeAsync(stream, this, Constants.SerializerOptions, cancellation);
        }

        private bool IsValid()
        {
            return rpaDir.Exists;
        }

        public static Project Load()
        {
            var project = new Project(Environment.CurrentDirectory);
            if (!project.IsValid())
                throw new Exception("Could not load the project in the current directory because it does not exist or is corrupted");

            return project;
        }
    }

    class ProjectSettings
    {
        public bool OverwriteOnFetch { get; private set; }

        public void AlwaysOverwriteOnFetch()
        {
            OverwriteOnFetch = true;
        }
    }
}