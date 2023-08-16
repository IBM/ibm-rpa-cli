using System.Diagnostics.CodeAnalysis;
using static Joba.IBM.RPA.ChatbotSettings;

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
        public static RobotSettings Create(string type, string description, PropertyOptions properties)
        {
            return type switch
            {
                PackageSettings.TypeName => new PackageSettings { Description = description },
                ChatbotSettings.TypeName => ChatbotSettings.Create(description, properties),
                AttendedSettings.TypeName => new AttendedSettings { Description = description },
                UnattendedSettings.TypeName => UnattendedSettings.Create(description, properties),
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
    [JsonDerivedType(typeof(PackageSettings), typeDiscriminator: PackageSettings.TypeName)]
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

        internal virtual void EnsureValid()
        {
            if (Timeout == TimeSpan.Zero)
                throw new InvalidOperationException($"Robot 'timeout' configuration must be greater than zero.");
        }
    }

    internal class ChatbotSettings : RobotSettings
    {
        internal const string TypeName = "chatbot";
        internal ChatbotSettings() : base() { }

        internal string? Name { get; init; }
        internal string? Handle { get; init; }
        [JsonPropertyName("unlock-machine")]
        internal bool? UnlockMachine { get; init; }
        internal string? Greeting { get; init; }
        internal string? Style { get; init; }
        internal string[] Computers { get; init; } = Array.Empty<string>();

        internal override void EnsureValid()
        {
            base.EnsureValid();
            if (string.IsNullOrEmpty(Name))
                throw new InvalidOperationException($"The {TypeName} robot requires 'name' configuration. See https://ibm.github.io/ibm-rpa-cli/#/guide/robot?id=chatbot.");
            if (string.IsNullOrEmpty(Handle))
                throw new InvalidOperationException($"The {TypeName} robot requires 'handle' configuration. See https://ibm.github.io/ibm-rpa-cli/#/guide/robot?id=chatbot.");
            if (Computers.Length == 0)
                throw new InvalidOperationException($"The {TypeName} robot requires at least one 'computer' configuration. See https://ibm.github.io/ibm-rpa-cli/#/guide/robot?id=chatbot.");
        }

        internal static ChatbotSettings Create(string description, PropertyOptions properties)
        {
            var handle = properties["handle"] ?? string.Empty;
            var name = properties["name"] ?? description;
            var greeting = properties["greeting"];
            var computers = properties["computers"] ?? string.Empty;
            return new ChatbotSettings
            {
                Description = description,
                Handle = handle,
                Name = name,
                Greeting = greeting,
                Computers = computers.Split(',')
            };
        }
    }

    internal class UnattendedSettings : RobotSettings
    {
        internal const string TypeName = "unattended";
        internal UnattendedSettings() : base() { }

        [JsonPropertyName("computer-group")]
        internal string? ComputerGroupName { get; set; }

        internal override void EnsureValid()
        {
            base.EnsureValid();
            if (string.IsNullOrEmpty(ComputerGroupName))
                throw new InvalidOperationException($"The {TypeName} robot requires 'computer-group' configuration. See https://ibm.github.io/ibm-rpa-cli/#/guide/robot?id=unattended.");
        }

        internal static UnattendedSettings Create(string description, PropertyOptions properties)
        {
            var handle = properties["computer-group"] ?? string.Empty;
            return new UnattendedSettings
            {
                Description = description,
                ComputerGroupName = handle,
            };
        }
    }

    internal class AttendedSettings : RobotSettings
    {
        internal const string TypeName = "attended";
        internal AttendedSettings() : base() { }
    }

    internal class PackageSettings : RobotSettings
    {
        internal const string TypeName = "package";
        internal PackageSettings() : base() { }
    }
}