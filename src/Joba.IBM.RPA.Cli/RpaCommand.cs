using System.CommandLine.Help;
using System.CommandLine.IO;

namespace Joba.IBM.RPA.Cli
{
    class RpaCommand : RootCommand
    {
        public RpaCommand() : base("Provides features to manage RPA through the command line")
        {
            var gitFile = new Argument<FileInfo?>("filePath", () => null,
                "The file that 'git' sends as parameter, so rpa can convert to text, in order to git compare changes.");

            AddArgument(gitFile);
            AddCommand(new ProjectCommand());
            AddCommand(new EnvironmentCommand());
            AddCommand(new StatusCommand());
            AddCommand(new PullCommand());
            AddCommand(new SwitchCommand());

            this.SetHandler(Handle, gitFile, Bind.FromServiceProvider<InvocationContext>());
        }

        private void Handle(FileInfo? gitFile, InvocationContext context)
        {
            if (gitFile != null)
                WriteFileContentToOutput(gitFile);
            else
                ShowHelp(context);
        }

        private static void WriteFileContentToOutput(FileInfo gitFile)
        {
            var wal = WalFile.ReadAllText(gitFile);
            Console.Write(wal);
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
