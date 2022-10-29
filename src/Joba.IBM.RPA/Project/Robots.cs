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

        bool IRobots.Exists(string name) => bots.ContainsKey(name);
        void IRobots.Add(string name, RobotSettings settings) => bots.Add(name, settings);
        public IEnumerator<Robot> GetEnumerator() => bots.Select(b => new Robot(b.Key, b.Value)).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public interface IRobots : IEnumerable<Robot>
    {
        bool Exists(string name);
        void Add(string name, RobotSettings settings);
    }

    public static class RobotSettingsFactory
    {
        public static RobotSettings Create(string type)
        {
            return type switch
            {
                ChatbotSettings.TypeName => new ChatbotSettings(),
                AttendedSettings.TypeName => new AttendedSettings(),
                UnattendedSettings.TypeName => new UnattendedSettings(),
                _ => throw new InvalidOperationException($"Type '{type}' is not supported."),
            };
        }
    }

    public record struct Robot(string Name, RobotSettings Settings);

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ChatbotSettings), typeDiscriminator: ChatbotSettings.TypeName)]
    [JsonDerivedType(typeof(UnattendedSettings), typeDiscriminator: UnattendedSettings.TypeName)]
    [JsonDerivedType(typeof(AttendedSettings), typeDiscriminator: AttendedSettings.TypeName)]
    public abstract class RobotSettings
    {
        internal RobotSettings() { }
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
    }

    internal class AttendedSettings : RobotSettings
    {
        internal const string TypeName = "attended";
        internal AttendedSettings() : base() { }
    }
}