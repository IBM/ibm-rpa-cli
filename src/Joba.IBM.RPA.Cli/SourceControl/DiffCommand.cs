using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class DiffCommand : Command
    {
        internal const string CommandName = "diff";

        internal DiffCommand() : base(CommandName, "Allows comparing two wal files")
        {
            var leftFile = new Argument<FileInfo>("leftFilePath", "The left file to compare. It accepts the full path or relative to the working directory.");
            var rightFile = new Argument<FileInfo>("rightFilePath", "The right file to compare. It accepts the full path or relative to the working directory.");

            AddArgument(leftFile);
            AddArgument(rightFile);

            this.SetHandler(HandleAsync, leftFile, rightFile, Bind.FromLogger<DiffCommand>(), Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(FileInfo leftFile, FileInfo rightFile, ILogger<DiffCommand> logger, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var workingDirectory = System.Environment.CurrentDirectory;
            var leftPath = leftFile.FullName;
            var rightPath = rightFile.FullName;
            if (Path.IsPathFullyQualified(leftPath) is false)
                leftPath = Path.Combine(workingDirectory, leftPath);
            if (Path.IsPathFullyQualified(rightPath) is false)
                rightPath = Path.Combine(workingDirectory, rightPath);

            leftFile = new FileInfo(leftPath);
            rightFile = new FileInfo(rightPath);

            var leftWal = WalFile.Read(leftFile);
            using var leftTxt = await TempFile.CreateAsync(leftWal, "left", cancellation);
            logger.LogDebug("Temp created for left {File}", leftTxt.Info);

            var rightWal = WalFile.Read(rightFile);
            using var rightTxt = await TempFile.CreateAsync(rightWal, "right", cancellation);
            logger.LogDebug("Temp created for right {File}", rightTxt.Info);

            var vsCode = new VsCode();
            logger.LogDebug("Launching Vs Code...");
            await vsCode.DiffAsync(leftTxt, rightTxt, cancellation);
        }
    }
}
