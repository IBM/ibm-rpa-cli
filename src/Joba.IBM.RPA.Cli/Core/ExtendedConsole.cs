using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joba.IBM.RPA.Cli
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

            private static string? Truncate(string? value, int maxChars) =>
                string.IsNullOrEmpty(value) ? value :
                value.Length <= maxChars ? value :
                value[..maxChars] + "...";
        }

        /// <summary>
        /// https://github.com/khalidabuhakmeh/ConsoleTables
        /// </summary>
        //class ConsoleTable
        //{
        //    public IList<object> Columns { get; set; }
        //    public IList<object[]> Rows { get; protected set; }

        //    public ConsoleTableOptions Options { get; protected set; }
        //    public Type[] ColumnTypes { get; private set; }

        //    public static HashSet<Type> NumericTypes = new HashSet<Type>
        //{
        //    typeof(int),  typeof(double),  typeof(decimal),
        //    typeof(long), typeof(short),   typeof(sbyte),
        //    typeof(byte), typeof(ulong),   typeof(ushort),
        //    typeof(uint), typeof(float)
        //};

        //    public ConsoleTable(params string[] columns)
        //        : this(new ConsoleTableOptions { Columns = new List<string>(columns) })
        //    {
        //    }

        //    public ConsoleTable(ConsoleTableOptions options)
        //    {
        //        Options = options ?? throw new ArgumentNullException("options");
        //        Rows = new List<object[]>();
        //        Columns = new List<object>(options.Columns);
        //    }

        //    public ConsoleTable AddColumn(IEnumerable<string> names)
        //    {
        //        foreach (var name in names)
        //            Columns.Add(name);
        //        return this;
        //    }

        //    public ConsoleTable AddRow(params object[] values)
        //    {
        //        if (values == null)
        //            throw new ArgumentNullException(nameof(values));

        //        if (!Columns.Any())
        //            throw new Exception("Please set the columns first");

        //        if (Columns.Count != values.Length)
        //            throw new Exception(
        //                $"The number columns in the row ({Columns.Count}) does not match the values ({values.Length})");

        //        Rows.Add(values);
        //        return this;
        //    }

        //    public ConsoleTable Configure(Action<ConsoleTableOptions> action)
        //    {
        //        action(Options);
        //        return this;
        //    }

        //    public static ConsoleTable From<T>(IEnumerable<T> values)
        //    {
        //        var table = new ConsoleTable
        //        {
        //            ColumnTypes = GetColumnsType<T>().ToArray()
        //        };

        //        var columns = GetColumns<T>();

        //        table.AddColumn(columns);

        //        foreach (
        //            var propertyValues
        //            in values.Select(value => columns.Select(column => GetColumnValue<T>(value, column)))
        //        ) table.AddRow(propertyValues.ToArray());

        //        return table;
        //    }

        //    public override string ToString()
        //    {
        //        var builder = new StringBuilder();

        //        // find the longest column by searching each row
        //        var columnLengths = ColumnLengths();

        //        // set right alinment if is a number
        //        var columnAlignment = Enumerable.Range(0, Columns.Count)
        //            .Select(GetNumberAlignment)
        //            .ToList();

        //        // create the string format with padding
        //        var format = Enumerable.Range(0, Columns.Count)
        //            .Select(i => " | {" + i + "," + columnAlignment[i] + columnLengths[i] + "}")
        //            .Aggregate((s, a) => s + a) + " |";

        //        // find the longest formatted line
        //        var maxRowLength = Math.Max(0, Rows.Any() ? Rows.Max(row => string.Format(format, row).Length) : 0);
        //        var columnHeaders = string.Format(format, Columns.ToArray());

        //        // longest line is greater of formatted columnHeader and longest row
        //        var longestLine = Math.Max(maxRowLength, columnHeaders.Length);

        //        // add each row
        //        var results = Rows.Select(row => string.Format(format, row)).ToList();

        //        // create the divider
        //        var divider = " " + string.Join("", Enumerable.Repeat("-", longestLine - 1)) + " ";

        //        builder.AppendLine(divider);
        //        builder.AppendLine(columnHeaders);

        //        foreach (var row in results)
        //        {
        //            builder.AppendLine(divider);
        //            builder.AppendLine(row);
        //        }

        //        builder.AppendLine(divider);

        //        if (Options.EnableCount)
        //        {
        //            builder.AppendLine("");
        //            builder.AppendFormat(" Count: {0}", Rows.Count);
        //        }

        //        return builder.ToString();
        //    }

        //    public string ToMarkDownString()
        //    {
        //        return ToMarkDownString('|');
        //    }

        //    private string ToMarkDownString(char delimiter)
        //    {
        //        var builder = new StringBuilder();

        //        // find the longest column by searching each row
        //        var columnLengths = ColumnLengths();

        //        // create the string format with padding
        //        var format = Format(columnLengths, delimiter);

        //        // find the longest formatted line
        //        var columnHeaders = string.Format(format, Columns.ToArray());

        //        // add each row
        //        var results = Rows.Select(row => string.Format(format, row)).ToList();

        //        // create the divider
        //        var divider = Regex.Replace(columnHeaders, @"[^|]", "-");

        //        builder.AppendLine(columnHeaders);
        //        builder.AppendLine(divider);
        //        results.ForEach(row => builder.AppendLine(row));

        //        return builder.ToString();
        //    }

        //    public string ToMinimalString()
        //    {
        //        return ToMarkDownString(char.MinValue);
        //    }

        //    public string ToStringAlternative()
        //    {
        //        var builder = new StringBuilder();

        //        // find the longest column by searching each row
        //        var columnLengths = ColumnLengths();

        //        // create the string format with padding
        //        var format = Format(columnLengths);

        //        // find the longest formatted line
        //        var columnHeaders = string.Format(format, Columns.ToArray());

        //        // add each row
        //        var results = Rows.Select(row => string.Format(format, row)).ToList();

        //        // create the divider
        //        var divider = Regex.Replace(columnHeaders, @"[^|]", "-");
        //        var dividerPlus = divider.Replace("|", "+");

        //        builder.AppendLine(dividerPlus);
        //        builder.AppendLine(columnHeaders);

        //        foreach (var row in results)
        //        {
        //            builder.AppendLine(dividerPlus);
        //            builder.AppendLine(row);
        //        }
        //        builder.AppendLine(dividerPlus);

        //        return builder.ToString();
        //    }

        //    private string Format(List<int> columnLengths, char delimiter = '|')
        //    {
        //        // set right alinment if is a number
        //        var columnAlignment = Enumerable.Range(0, Columns.Count)
        //            .Select(GetNumberAlignment)
        //            .ToList();

        //        var delimiterStr = delimiter == char.MinValue ? string.Empty : delimiter.ToString();
        //        var format = (Enumerable.Range(0, Columns.Count)
        //            .Select(i => " " + delimiterStr + " {" + i + "," + columnAlignment[i] + columnLengths[i] + "}")
        //            .Aggregate((s, a) => s + a) + " " + delimiterStr).Trim();
        //        return format;
        //    }

        //    private string GetNumberAlignment(int i)
        //    {
        //        return Options.NumberAlignment == Alignment.Right
        //                && ColumnTypes != null
        //                && NumericTypes.Contains(ColumnTypes[i])
        //            ? ""
        //            : "-";
        //    }

        //    private List<int> ColumnLengths()
        //    {
        //        var columnLengths = Columns
        //            .Select((t, i) => Rows.Select(x => x[i])
        //                .Union(new[] { Columns[i] })
        //                .Where(x => x != null)
        //                .Select(x => x.ToString().Length).Max())
        //            .ToList();
        //        return columnLengths;
        //    }

        //    public void Write(Format format = ConsoleTables.Format.Default)
        //    {
        //        switch (format)
        //        {
        //            case ConsoleTables.Format.Default:
        //                Options.OutputTo.WriteLine(ToString());
        //                break;
        //            case ConsoleTables.Format.MarkDown:
        //                Options.OutputTo.WriteLine(ToMarkDownString());
        //                break;
        //            case ConsoleTables.Format.Alternative:
        //                Options.OutputTo.WriteLine(ToStringAlternative());
        //                break;
        //            case ConsoleTables.Format.Minimal:
        //                Options.OutputTo.WriteLine(ToMinimalString());
        //                break;
        //            default:
        //                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        //        }
        //    }

        //    private static IEnumerable<string> GetColumns<T>()
        //    {
        //        return typeof(T).GetProperties().Select(x => x.Name).ToArray();
        //    }

        //    private static object GetColumnValue<T>(object target, string column)
        //    {
        //        return typeof(T).GetProperty(column).GetValue(target, null);
        //    }

        //    private static IEnumerable<Type> GetColumnsType<T>()
        //    {
        //        return typeof(T).GetProperties().Select(x => x.PropertyType).ToArray();
        //    }
        //}
    }
}
