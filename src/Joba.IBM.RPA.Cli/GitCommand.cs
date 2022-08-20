namespace Joba.IBM.RPA.Cli
{
    class GitCommand : Command
    {
        public GitCommand() : base("git", "Provides integration with git")
        {
            //TODO: git merge support: https://git-scm.com/docs/git-mergetool
            AddCommand(new DiffCommand());
        }

        /// <summary>
        /// git diff support: https://git-scm.com/book/en/v2/Customizing-Git-Git-Attributes#Binary-Files
        /// </summary>
        class DiffCommand : Command
        {
            public DiffCommand() : base("diff", "Converts the binary wal file to plain text for 'git diff'")
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
