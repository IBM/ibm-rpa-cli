using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace Joba.IBM.RPA.Cli
{
    internal class RpaCommand : RootCommand
    {
        public const string CommandName = "rpa";
        public static readonly Option<Verbosity> VerbosityOption = CreateVerbosityOption();

        private static Option<Verbosity> CreateVerbosityOption()
        {
            var verbosity = new Option<Verbosity>("--verbosity", ParseVerbosity, isDefault: true, description: "Specifies how much output is sent to the console.")
               .FromAmong(Verbosity.Quiet, Verbosity.Minimal, Verbosity.Normal, Verbosity.Detailed, Verbosity.Diagnostic);
            verbosity.AddAlias("-v");
            verbosity.AddValidator(ValidateVerbosity);
            return verbosity;
        }

        private static Verbosity ParseVerbosity(ArgumentResult result)
        {
            if (result.Tokens.Count == 1)
                return new Verbosity(result.Tokens[0].Value);

            if (result.Tokens.Count == 1 && result.Tokens[0].Value == "-v")
                return Verbosity.Diagnostic;

#if DEBUG
            return Verbosity.Diagnostic;
#else
            return Verbosity.Normal;
#endif
        }

        private static void ValidateVerbosity(OptionResult result)
        {
            //not called
        }

        public RpaCommand() : base("Provides features to manage RPA through the command line")
        {
            AddGlobalOption(VerbosityOption);
            AddCommand(new ProjectCommand());
            AddCommand(new EnvironmentCommand());
            AddCommand(new StatusCommand());
            AddCommand(new PullCommand());
            AddCommand(new PushCommand());
            AddCommand(new SwitchCommand());
            AddCommand(new PackageCommand());
            AddCommand(new GitCommand());

            this.SetHandler(ShowHelp, Bind.FromServiceProvider<InvocationContext>());
        }

        private static void ShowHelp(InvocationContext context)
        {
            using var output = context.Console.Out.CreateTextWriter();
            var helpContext = new HelpContext(context.HelpBuilder,
                                              context.ParseResult.CommandResult.Command,
                                              output,
                                              context.ParseResult);

            context.HelpBuilder.Write(helpContext);
        }
    }

    internal struct Verbosity
    {
        internal static readonly Verbosity Quiet = new(nameof(Quiet));
        internal static readonly Verbosity Minimal = new(nameof(Minimal));
        internal static readonly Verbosity Normal = new(nameof(Normal));
        internal static readonly Verbosity Detailed = new(nameof(Detailed));
        internal static readonly Verbosity Diagnostic = new(nameof(Diagnostic));
        private static readonly IDictionary<string, LogLevel> mappings = new Dictionary<string, LogLevel>
        {
            [Quiet] = LogLevel.None,
            [Minimal] = LogLevel.Warning,
            [Normal] = LogLevel.Information,
            [Detailed] = LogLevel.Debug,
            [Diagnostic] = LogLevel.Trace,
        };
        private readonly string value;

        internal Verbosity(string? value) => this.value = value ?? Diagnostic;

        internal LogLevel ToLogLevel() => mappings[value];
        public override string ToString() => value;
        public static implicit operator string(Verbosity verbosity) => verbosity.ToString();
    }
}
