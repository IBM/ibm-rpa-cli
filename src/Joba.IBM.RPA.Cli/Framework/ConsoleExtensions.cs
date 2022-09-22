using System.CommandLine.IO;

namespace Joba.IBM.RPA.Cli
{
    internal static class ConsoleExtensions
    {
        internal static string? ReadLine(this IConsole _) => Console.ReadLine();

        internal static bool? YesOrNo(this IConsole console, string value)
        {
            console.WriteLine(value);

            var keyInfo = Console.ReadKey(true);
            return keyInfo.Key switch
            {
                ConsoleKey.Y => true,
                ConsoleKey.N => false,
                _ => null,
            };
        }

        internal static string Password(this IConsole console)
        {
            var result = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        console.Out.WriteLine();
                        return result.ToString();
                    case ConsoleKey.Backspace:
                        if (result.Length == 0)
                            continue;

                        result.Length--;
                        console.Write("\b \b");
                        continue;
                    default:
                        result.Append(key.KeyChar);
                        console.Write("*");
                        continue;
                }
            }
        }

        internal static int? ShowMenu(this IConsole console, string title, params string[] options)
        {
            var menu = new ConsoleMenu(console, options);
            return menu.Run(title);
        }

        internal static void WriteLineIndented(this IConsole console, string value)
        {
            console.Write("\t");
            console.WriteLine(value);
        }

        internal static void WriteLineIndented(this IConsole console, string value, int padding)
        {
            console.Write(new string(' ', padding));
            console.WriteLine(value);
        }

        internal static void WriteWarningLine(this IConsole console, string value)
        {
            using var _ = console.BeginForegroundColor(ConsoleColor.Yellow);
            console.WriteLine(value);
        }

        internal static IDisposable BeginForegroundColor(this IConsole _, ConsoleColor color) => new ConsoleForegroundColor(color);

        /// <summary>
        /// Copied from https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Console/src/TextWriterExtensions.cs
        /// </summary>
        internal static void WriteColoredMessage(this TextWriter textWriter, string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            // Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
            if (background.HasValue)
            {
                textWriter.Write(AnsiParser.GetBackgroundColorEscapeCode(background.Value));
            }
            if (foreground.HasValue)
            {
                textWriter.Write(AnsiParser.GetForegroundColorEscapeCode(foreground.Value));
            }
            textWriter.Write(message);
            if (foreground.HasValue)
            {
                textWriter.Write(AnsiParser.DefaultForegroundColor); // reset to default foreground color
            }
            if (background.HasValue)
            {
                textWriter.Write(AnsiParser.DefaultBackgroundColor); // reset to the background color
            }
        }

        class ConsoleForegroundColor : IDisposable
        {
            private readonly ConsoleColor previousForegroundColor;

            public ConsoleForegroundColor(ConsoleColor color)
            {
                previousForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
            }

            void IDisposable.Dispose()
            {
                Console.ForegroundColor = previousForegroundColor;
            }
        }

        class ConsoleMenu
        {
            private readonly IConsole console;
            private readonly string[] options;

            public ConsoleMenu(IConsole console, string[] options)
            {
                this.console = console;
                this.options = options;
            }

            public int? Run(string title)
            {
                Console.CursorVisible = false;
                var index = 0;
                RenderMenu(title, index);
                ConsoleKeyInfo keyinfo;

                do
                {
                    keyinfo = Console.ReadKey();

                    // Handle each key input (down arrow will write the menu again with a different selected item)
                    if (keyinfo.Key == ConsoleKey.DownArrow)
                    {
                        if (index + 1 < options.Length)
                        {
                            index++;
                            RenderMenu(title, index);
                        }
                    }
                    if (keyinfo.Key == ConsoleKey.UpArrow)
                    {
                        if (index - 1 >= 0)
                        {
                            index--;
                            RenderMenu(title, index);
                        }
                    }
                    // Handle different action for the option
                    if (keyinfo.Key == ConsoleKey.Enter)
                        return index;
                }
                while (keyinfo.Key != ConsoleKey.X && keyinfo.Key != ConsoleKey.Escape);

                Console.CursorVisible = true;
                return null;
            }

            private void RenderMenu(string title, int selectedOption)
            {
                Console.Clear();

                if (!string.IsNullOrEmpty(title))
                    console.WriteLine($"{title} {System.Environment.NewLine}");

                for (var i = 0; i < options.Length; i++)
                {
                    var option = options[i];

                    if (i == selectedOption)
                    {
                        //Console.BackgroundColor = ConsoleColor.Gray;
                        //Console.ForegroundColor = ConsoleColor.Black;
                        console.Write("> ");
                    }
                    else
                    {
                        console.Write("  ");
                        //Console.ResetColor();
                    }

                    console.WriteLine($" {Truncate(option, 30)}");
                }
            }

            private static string? Truncate(string? value, int maxChars) =>
                string.IsNullOrEmpty(value) ? value :
                value.Length <= maxChars ? value :
                value[..maxChars] + "...";
        }
    }
}
