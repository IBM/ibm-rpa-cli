using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joba.IBM.RPA
{
    internal static class ConsoleExtensions
    {
        public static void SetTerminalForegroundColor(this IConsole console, ConsoleColor color)
        {
            if (console.GetType().GetInterfaces().Any(i => i.Name == "ITerminal"))
                ((dynamic)console).ForegroundColor = color;

            if (IsConsoleRedirectionCheckSupported)
                Console.ForegroundColor = color;
        }

        public static void ResetTerminalForegroundColor(this IConsole console)
        {
            if (console.GetType().GetInterfaces().Any(i => i.Name == "ITerminal"))
                ((dynamic)console).ForegroundColor = ConsoleColor.Red;

            if (IsConsoleRedirectionCheckSupported)
                Console.ResetColor();
        }

        private static bool? isConsoleRedirectionCheckSupported;
        private static bool IsConsoleRedirectionCheckSupported
        {
            get
            {
                if (isConsoleRedirectionCheckSupported != null)
                    return isConsoleRedirectionCheckSupported.Value;

                try
                {
                    var check = Console.IsOutputRedirected;
                    isConsoleRedirectionCheckSupported = true;
                }

                catch (PlatformNotSupportedException)
                {
                    isConsoleRedirectionCheckSupported = false;
                }

                return isConsoleRedirectionCheckSupported.Value;
            }
        }
    }
}
