using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joba.IBM.RPA
{
    internal static class ConsoleHelper
    {
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
