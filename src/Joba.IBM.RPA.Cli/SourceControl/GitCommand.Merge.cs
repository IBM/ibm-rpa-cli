using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    internal partial class GitCommand
    {
        /// <summary>
        /// Following the <a href="https://git-scm.com/docs/gitattributes#_defining_a_custom_merge_driver">documentation</a> to create a custom merge driver.
        /// See <see cref="GitConfigurator"/> to understand how we configure .gitattributes and .gitconfig files.
        /// </summary>
        internal class MergeCommand : Command
        {
            internal const string CommandName = "merge";
            public MergeCommand() : base(CommandName, "Help git merging wal files. If conflicts are detected, then Visual Studio Code is used to resolve them.")
            {
                var baseFile = new Argument<FileInfo>("base", "File containing the common base for the merge.");
                var localFile = new Argument<FileInfo>("local", "File containing the contents of the file on the current branch.");
                var remoteFile = new Argument<FileInfo>("remote", "File containing the contents of the file to be merged.");
                var mergedFile = new Argument<FileInfo?>("merged", "The name of the file to which the merge tool should write the result of the merge resolution. If this is not passed, the results will be written to the 'local' file.") { Arity = ArgumentArity.ZeroOrOne };

                AddArgument(baseFile);
                AddArgument(localFile);
                AddArgument(remoteFile);
                AddArgument(mergedFile);
                this.SetHandler(HandleAsync, baseFile, localFile, remoteFile, mergedFile,
                    Bind.FromLogger<GitCommand>(),
                    Bind.FromServiceProvider<InvocationContext>());
            }

            private async Task HandleAsync(FileInfo baseFile, FileInfo localFile, FileInfo remoteFile, FileInfo? mergedFile,
                ILogger<GitCommand> logger, InvocationContext context)
            {
                logger.LogDebug("Files: base={base} | local={local} | remote={remote} | merged={merged}", baseFile, localFile, remoteFile, mergedFile?.FullName ?? "<null>");

                var cancellation = context.GetCancellationToken();
                mergedFile ??= localFile; //if 'merged' was not provided, then save the merge result back to 'local'
                baseFile = new FileInfo(Path.GetFullPath(baseFile.FullName));
                localFile = new FileInfo(Path.GetFullPath(localFile.FullName));
                remoteFile = new FileInfo(Path.GetFullPath(remoteFile.FullName));
                mergedFile = new FileInfo(Path.GetFullPath(mergedFile.FullName));

                //TODO: not working... the 'base' file is corrupted by git :(
                logger.LogDebug("Reading base {File} (exists={Exists})", baseFile, baseFile.Exists);
                //Console.ReadLine();
                var baseWal = WalFile.Read(baseFile);
                logger.LogDebug("Reading local {File}", localFile);
                var localWal = WalFile.Read(localFile);
                logger.LogDebug("Reading remote {File}", remoteFile);
                var remoteWal = WalFile.Read(remoteFile);
                var mergedWal = mergedFile.Exists ? WalFile.Read(mergedFile) : remoteWal.CloneTo(mergedFile);

                using var baseTxt = await TempFile.CreateAsync(baseWal, "base", cancellation);
                logger.LogDebug("Temp created for base {File}", baseTxt.Info);
                using var localTxt = await TempFile.CreateAsync(localWal, "local", cancellation);
                logger.LogDebug("Temp created for local {File}", localTxt.Info);
                using var remoteTxt = await TempFile.CreateAsync(remoteWal, "remote", cancellation);
                logger.LogDebug("Temp created for remote {File}", remoteTxt.Info);
                using var mergedTxt = await TempFile.CreateAsync(mergedWal, "merged", cancellation);
                logger.LogDebug("Temp created for merged {File}", mergedTxt.Info);

                var vsCode = new VsCode();
                logger.LogDebug("Launching Vs Code...");
                await vsCode.MergeAsync(localTxt, remoteTxt, baseTxt, mergedTxt, cancellation);

                mergedWal.Overwrite(new WalContent(await mergedTxt.ReadAsync(cancellation)));
            }
        }
    }
}
