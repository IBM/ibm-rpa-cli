using System.CommandLine.Help;
using System.CommandLine.IO;

namespace Joba.IBM.RPA.Cli
{
    class RpaCommand : RootCommand
    {
        public static readonly string CommandName = "rpa";
        public RpaCommand() : base("Provides features to manage RPA through the command line")
        {
            AddCommand(new ProjectCommand());
            AddCommand(new EnvironmentCommand());
            AddCommand(new StatusCommand());
            AddCommand(new PullCommand());
            AddCommand(new SwitchCommand());
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
}
