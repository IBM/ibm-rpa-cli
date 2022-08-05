using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joba.IBM.RPA
{
    internal class ExtendedConsole : IFormatProvider, ICustomFormatter
    {
        private const char ColorSeparator = ':';
        private const char Separator = (char)65535;//non-printable character

        private static readonly Type CustomFormatterType;
        private static readonly Dictionary<string, ConsoleColor> Colors;
        private static readonly ExtendedConsole This = new();

        static ExtendedConsole()
        {
            CustomFormatterType = typeof(ICustomFormatter);
            Colors = Enum.GetValues<ConsoleColor>().ToDictionary(x => x.ToString().ToLowerInvariant(), x => x);
        }

        object? IFormatProvider.GetFormat(Type? formatType)
        {
            return CustomFormatterType == formatType ? this : null;
        }

        private static ConsoleColor? GetColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) return null;
            if (Colors.TryGetValue(color.Trim().ToLowerInvariant(), out var consoleColor))
            {
                return consoleColor;
            }

            throw new Exception($"System.ConsoleColor enum does not have a member named {color}");
        }

        private static (ConsoleColor? foreground, ConsoleColor? background)? GetColors(string colors)
        {
            if (string.IsNullOrWhiteSpace(colors)) return null;
            if (colors.Contains(ColorSeparator))
            {
                var split = colors.Split(ColorSeparator);
                var foreground = GetColor(split[0]);
                var background = GetColor(split[1]);
                return (foreground, background);
            }

            var single = GetColor(colors);
            return (single, null);
        }

        string ICustomFormatter.Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (format == null) return arg?.ToString();
            var sb = new StringBuilder();
            sb.Append(Separator);
            sb.Append(format);
            sb.Append(Separator);
            sb.Append(arg);
            sb.Append(Separator);
            return sb.ToString();
        }

        public static void WriteLine(FormattableString f)
        {
            Write(f);
            Console.WriteLine();
        }

        public static void Write(FormattableString f)
        {
            lock (This)
            {
                WriteInternal(f);
            }
        }

        private static void WriteInternal(FormattableString f)
        {
            var str = f.ToString(This);
            var sb = new StringBuilder();
            var format = new StringBuilder();
            var arg = new StringBuilder();
            bool? state = null;//null->normal,true->separator start,2->separator end

            var defaultForegroundColor = Console.ForegroundColor;
            var defaultBackgroundColor = Console.BackgroundColor;

            foreach (var ch in str)
            {
                switch (state)
                {
                    case null when ch == Separator:
                        Print(null, null, sb.ToString());
                        sb.Clear();
                        state = true;
                        break;
                    case null:
                        sb.Append(ch);
                        break;
                    case true when ch == Separator:
                        state = false;
                        break;
                    case true:
                        format.Append(ch);
                        break;
                    case false when ch == Separator:
                        state = null;
                        var colors = GetColors(format.ToString());
                        Print(colors?.foreground, colors?.background, arg.ToString());
                        format.Clear();
                        arg.Clear();
                        break;
                    case false:
                        arg.Append(ch);
                        break;
                }
            }

            if (format.Length > 0 && arg.Length > 0)
            {
                var colors = GetColors(format.ToString());
                Print(colors?.foreground, colors?.background, arg.ToString());
                format.Clear();
            }

            if (sb.Length > 0)
                Print(null, null, sb.ToString());

            Console.ForegroundColor = defaultForegroundColor;
            Console.BackgroundColor = defaultBackgroundColor;

            void Print(ConsoleColor? foreground, ConsoleColor? background, string text)
            {
                Console.ForegroundColor = foreground ?? defaultForegroundColor;
                Console.BackgroundColor = background ?? defaultBackgroundColor;
                Console.Write(text);
            }
        }

        public static string Password()
        {
            var result = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return result.ToString();
                    case ConsoleKey.Backspace:
                        if (result.Length == 0)
                            continue;

                        result.Length--;
                        Console.Write("\b \b");
                        continue;
                    default:
                        result.Append(key.KeyChar);
                        Console.Write("*");
                        continue;
                }
            }
        }

        public static int? ShowMenu(string title, params string[] options)
        {
            var menu = new ConsoleMenu(title, options);
            return menu.Run();
        }

        class ConsoleMenu
        {
            private readonly string title;
            private readonly string[] options;

            public ConsoleMenu(string title, string[] options)
            {
                this.title = title;
                this.options = options;
            }

            public int? Run()
            {
                Console.CursorVisible = false;
                var index = 0;
                RenderMenu(index);
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
                            RenderMenu(index);
                        }
                    }
                    if (keyinfo.Key == ConsoleKey.UpArrow)
                    {
                        if (index - 1 >= 0)
                        {
                            index--;
                            RenderMenu(index);
                        }
                    }
                    // Handle different action for the option
                    if (keyinfo.Key == ConsoleKey.Enter)
                        return index;
                }
                while (keyinfo.Key != ConsoleKey.X && keyinfo.Key != ConsoleKey.Escape);

                Console.CursorVisible = true;
                return null;
                //Console.ReadKey();
            }

            private void RenderMenu(int selectedOption)
            {
                Console.Clear();

                if (!string.IsNullOrEmpty(title))
                    Console.WriteLine($"{title}: {Environment.NewLine}");

                for (var i = 0; i < options.Length; i++)
                {
                    var option = options[i];

                    if (i == selectedOption)
                    {
                        //Console.BackgroundColor = ConsoleColor.Gray;
                        //Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write("> ");
                    }
                    else
                    {
                        Console.Write("  ");
                        //Console.ResetColor();
                    }

                    Console.WriteLine(" {0}", Truncate(option, 27));
                }
            }

            private static string? Truncate(string? value, int maxChars) =>
                string.IsNullOrEmpty(value) ? value :
                value.Length <= maxChars ? value :
                value[..maxChars] + "...";
        }
    }
}
