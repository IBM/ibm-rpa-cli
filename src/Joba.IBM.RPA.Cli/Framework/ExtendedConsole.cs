using System.Text;

namespace Joba.IBM.RPA.Cli
{
    internal class ExtendedConsole
    {
        public static IDisposable BeginForegroundColor(ConsoleColor color)
        {
            return new ConsoleForegroundColor(color);
        }

        public static void WriteWarningLine(ref ConsoleInterpolatedStringHandler builder)
        {
            using (BeginForegroundColor(ConsoleColor.Yellow))
                builder.WriteLine();
        }

        public static bool? YesOrNo(ref ConsoleInterpolatedStringHandler builder, ConsoleColor? color = null)
        {
            using (BeginForegroundColor(color ?? Console.ForegroundColor))
            {
                builder.WriteLine();
                var keyInfo = Console.ReadKey(true);
                return keyInfo.Key switch
                {
                    ConsoleKey.Y => true,
                    ConsoleKey.N => false,
                    _ => null,
                };
            }
        }

        public static void WriteLine(ref ConsoleInterpolatedStringHandler builder) => builder.WriteLine();

        public static void Write(ref ConsoleInterpolatedStringHandler builder) => builder.Write();

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

        public static int? ShowMenu(ref ConsoleInterpolatedStringHandler builder, params string[] options)
        {
            var menu = new ConsoleMenu(options);
            return menu.Run(ref builder);
        }

        public static int? ShowMenu(string title, params string[] options)
        {
            var menu = new ConsoleMenu(options);
            return menu.Run(title);
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
            private readonly string[] options;

            public ConsoleMenu(string[] options)
            {
                this.options = options;
            }

            public int? Run(ref ConsoleInterpolatedStringHandler builder)
            {
                Console.CursorVisible = false;
                var index = 0;
                RenderMenu(ref builder, index);
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
                            RenderMenu(ref builder, index);
                        }
                    }
                    if (keyinfo.Key == ConsoleKey.UpArrow)
                    {
                        if (index - 1 >= 0)
                        {
                            index--;
                            RenderMenu(ref builder, index);
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
                    Console.WriteLine($"{title} {System.Environment.NewLine}");

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

            private void RenderMenu(ref ConsoleInterpolatedStringHandler builder, int selectedOption)
            {
                Console.Clear();

                builder.WriteLine();

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
