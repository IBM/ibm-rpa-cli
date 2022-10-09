namespace Joba.IBM.RPA.Cli
{
    internal partial class GitCommand
    {
        /// <summary>
        /// Read the following <a href="https://git-scm.com/book/en/v2/Customizing-Git-Git-Attributes#Binary-Files">documentation</a> to learn how to tell git to handle binary files.
        /// See <see cref="GitConfigurator"/> to understand how we configure .gitattributes and .gitconfig files.
        /// </summary>
        internal class DiffCommand : Command
        {
            internal const string CommandName = "diff";
            public DiffCommand() : base(CommandName, "Converts the binary wal file to plain text for 'git diff'")
            {
                var gitFile = new Argument<FileInfo>("filePath",
                    "The file that 'git' sends as parameter, so rpa can convert to text, in order to git compare changes.");

                AddArgument(gitFile);
                this.SetHandler(Handle, gitFile, Bind.FromServiceProvider<InvocationContext>());
            }

            private void Handle(FileInfo gitFile, InvocationContext context)
            {
                var wal = WalFile.ReadAllText(gitFile);
                Console.Write(wal);
            }
        }
    }
}
