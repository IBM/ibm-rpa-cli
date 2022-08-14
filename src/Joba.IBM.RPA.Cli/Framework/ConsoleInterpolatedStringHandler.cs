using System.Runtime.CompilerServices;

namespace Joba.IBM.RPA.Cli
{
    [InterpolatedStringHandler]
    public ref struct ConsoleInterpolatedStringHandler
    {
        private static readonly Dictionary<string, ConsoleColor> colors;
        private readonly IList<Action> actions;

        static ConsoleInterpolatedStringHandler() =>
            colors = Enum.GetValues<ConsoleColor>().ToDictionary(x => x.ToString().ToLowerInvariant(), x => x);

        public ConsoleInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            actions = new List<Action>();
        }

        public void AppendLiteral(string s)
        {
            actions.Add(() => Console.Write(s));
        }

        public void AppendFormatted<T>(T t)
        {
            actions.Add(() => Console.Write(t));
        }

        public void AppendFormatted<T>(T t, string format)
        {
            if (!colors.TryGetValue(format, out var color))
                throw new InvalidOperationException($"Color '{format}' not supported");

            actions.Add(() =>
            {
                using (ExtendedConsole.BeginForegroundColor(color))
                    Console.Write(t);
            });
        }

        internal void WriteLine() => Write(true);
        internal void Write() => Write(false);

        private void Write(bool newLine)
        {
            foreach (var action in actions)
                action();

            if (newLine)
                Console.WriteLine();
        }
    }
}
