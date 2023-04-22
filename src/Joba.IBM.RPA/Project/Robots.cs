using System.Diagnostics.CodeAnalysis;

namespace Joba.IBM.RPA
{
    internal class Robots : IRobots
    {
        private readonly IDictionary<string, RobotSettings> bots;

        internal Robots()
            : this(new Dictionary<string, RobotSettings>()) { }

        internal Robots(IDictionary<string, RobotSettings> bots)
        {
            this.bots = bots;
        }

        Robot IRobots.this[string name] => new(name, bots[name]);
        bool IRobots.Exists(string name) => bots.ContainsKey(name);
        void IRobots.Add(string name, RobotSettings settings) => bots.Add(name, settings);
        public IEnumerator<Robot> GetEnumerator() => bots.Select(b => new Robot(b.Key, b.Value)).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int ICollection.Count => bots.Count;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;
        void ICollection.CopyTo(Array array, int index) => bots.CopyTo((KeyValuePair<string, RobotSettings>[])array, index);
    }

    public interface IRobots : IEnumerable<Robot>, ICollection
    {
        bool Exists(string name);
        void Add(string name, RobotSettings settings);
        Robot this[string name] { get; }
    }

    public static class RobotSettingsFactory
    {
        public static RobotSettings Create(string type, string description)
        {
            return type switch
            {
                ChatbotSettings.TypeName => new ChatbotSettings { Description = description },
                AttendedSettings.TypeName => new AttendedSettings { Description = description },
                UnattendedSettings.TypeName => new UnattendedSettings { Description = description },
                _ => new UnattendedSettings { Description = description }
            };
        }
    }

    public readonly struct Robot
    {
        public Robot(string name, RobotSettings settings)
        {
            Name = name;
            Settings = settings;
        }

        public string Name { get; init; }
        public RobotSettings Settings { get; init; }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null) return false;
            if (obj is Robot robot)
                return robot.Name == Name;
            return false;
        }

        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;
        public static bool operator ==(Robot left, Robot right) => left.Equals(right);
        public static bool operator !=(Robot left, Robot right) => !(left == right);
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ChatbotSettings), typeDiscriminator: ChatbotSettings.TypeName)]
    [JsonDerivedType(typeof(UnattendedSettings), typeDiscriminator: UnattendedSettings.TypeName)]
    [JsonDerivedType(typeof(AttendedSettings), typeDiscriminator: AttendedSettings.TypeName)]
    public abstract class RobotSettings
    {
        internal RobotSettings() { }

        internal TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        internal string Description { get; set; } = string.Empty;
        /// <summary>
        /// Specify wal files to include in the build process of 'this' robot.
        /// </summary>
        internal string[]? Include { get; set; }
    }

    internal class ChatbotSettings : RobotSettings
    {
        internal const string TypeName = "chatbot";
        internal ChatbotSettings() : base() { }
    }

    internal class UnattendedSettings : RobotSettings
    {
        internal const string TypeName = "unattended";
        internal UnattendedSettings() : base() { }

        [JsonPropertyName("computer-group")]
        internal string? ComputerGroupName { get; set; }
    }

    internal class AttendedSettings : RobotSettings
    {
        internal const string TypeName = "attended";
        internal AttendedSettings() : base() { }
    }
}