namespace Joba.IBM.RPA
{
    public sealed class BuildResult
    {
        private BuildResult(TimeSpan time, IDictionary<Robot, WalFile> robots)
        {
            Time = time;
            Robots = robots;
        }

        private BuildResult(TimeSpan time, Exception error)
        {
            Time = time;
            Error = error;
        }

        public TimeSpan Time { get; }
        public bool Success => Error == null;
        public IDictionary<Robot, WalFile> Robots { get; } = new Dictionary<Robot, WalFile>();
        public Exception? Error { get; }

        public string GetTimeAsString()
        {
            if (Time < TimeSpan.FromSeconds(1))
                return $"{Time.TotalMilliseconds}ms";
            return $"{Time.TotalSeconds}s";
        }

        internal static BuildResult Succeed(TimeSpan time, IDictionary<Robot, WalFile> robots) => new(time, robots);
        internal static BuildResult Succeed(TimeSpan time, Robot robot, WalFile wal) => new(time, new Dictionary<Robot, WalFile> { { robot, wal } });
        internal static BuildResult Failed(TimeSpan time, Exception error) => new(time, error);
    }
}
