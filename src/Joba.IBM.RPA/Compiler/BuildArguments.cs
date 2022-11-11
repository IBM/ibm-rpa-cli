namespace Joba.IBM.RPA
{
    public sealed class BuildArguments
    {
        public BuildArguments(IProject project, DirectoryInfo outputDirectory)
            : this(project, null, null, outputDirectory) { }

        public BuildArguments(IProject project, WalFileName[] include, DirectoryInfo outputDirectory)
            : this(project, null, include, outputDirectory) { }

        public BuildArguments(IProject project, Robot robot, DirectoryInfo outputDirectory)
            : this(project, robot, null, outputDirectory) { }

        public BuildArguments(IProject project, Robot? robot, WalFileName[]? include, DirectoryInfo outputDirectory)
        {
            Project = project;
            Robot = robot;
            Include = include;
            OutputDirectory = outputDirectory;
        }

        public IProject Project { get; }
        public Robot? Robot { get; }
        public WalFileName[]? Include { get; }
        public DirectoryInfo OutputDirectory { get; }
    }
}
