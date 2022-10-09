using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
                var cancellation = context.GetCancellationToken();
                logger.LogDebug("Files: base={base} | local={local} | remote={remote} | merged={merged}", baseFile, localFile, remoteFile, mergedFile?.FullName ?? "<null>");

                mergedFile ??= localFile; //if 'merged' was not provided, then save the merge result back to 'local'
                baseFile = new FileInfo(Path.GetFullPath(baseFile.FullName));
                localFile = new FileInfo(Path.GetFullPath(localFile.FullName));
                remoteFile = new FileInfo(Path.GetFullPath(remoteFile.FullName));
                mergedFile = new FileInfo(Path.GetFullPath(mergedFile.FullName));

                var baseWal = WalFile.Read(baseFile);
                var localWal = WalFile.Read(localFile);
                var remoteWal = WalFile.Read(remoteFile);
                var mergedWal = mergedFile.Exists ? WalFile.Read(mergedFile) : remoteWal.CloneTo(mergedFile);

                using var baseTxt = await TempFile.CreateAsync(baseWal, "base", cancellation);
                using var localTxt = await TempFile.CreateAsync(localWal, "local", cancellation);
                using var remoteTxt = await TempFile.CreateAsync(remoteWal, "remote", cancellation);
                using var mergedTxt = await TempFile.CreateAsync(mergedWal, "merged", cancellation);

                var vsCode = new VsCode();
                await vsCode.MergeAsync(localTxt, remoteTxt, baseTxt, mergedTxt, cancellation);

                mergedWal.Overwrite(new WalContent(await mergedTxt.ReadAsync(cancellation)));
            }

            class VsCode
            {
                private const string ExeName = "code";

                /// <summary>
                /// Launches a new session of VSCode to 3-way merge files, according to the <a href="https://code.visualstudio.com/docs/editor/command-line#_core-cli-options">documentation</a>.
                /// </summary>
                /// <param name="leftFile"></param>
                /// <param name="rightFile"></param>
                /// <param name="baseFile"></param>
                /// <param name="resultFile"></param>
                /// <param name="cancellation"></param>
                /// <returns></returns>
                /// <exception cref="Exception"></exception>
                internal async Task MergeAsync(FileInfo leftFile, FileInfo rightFile, FileInfo baseFile, FileInfo resultFile, CancellationToken cancellation)
                {
                    var arguments = $"-n -m \"{leftFile.FullName}\" \"{rightFile.FullName}\" \"{baseFile.FullName}\" \"{resultFile.FullName}\" --wait";
                    var info = new ProcessStartInfo(ExeName, arguments) { UseShellExecute = true, CreateNoWindow = true };
                    using var process = Process.Start(info);
                    if (process == null)
                        throw new Exception($"Could not start '{ExeName}' tool.");

                    await process.WaitForExitAsync(cancellation);
                }
            }

            class TempFile : IDisposable
            {
                private readonly FileInfo file;

                private TempFile(FileInfo file)
                {
                    this.file = file;
                }

                internal FileInfo Info => file;

                internal static async Task<TempFile> CreateAsync(WalFile wal, string prefix, CancellationToken cancellation)
                {
                    var tempDir = new DirectoryInfo(Path.GetTempPath());
                    if (!tempDir.Exists)
                        tempDir.Create();

                    var file = new FileInfo(Path.Combine(tempDir.FullName, $"[{prefix}] {wal.Info.Name}"));
                    await File.WriteAllTextAsync(file.FullName, wal.ToString(), cancellation);
                    return new TempFile(file);
                }

                internal async Task<string> ReadAsync(CancellationToken cancellation) => await File.ReadAllTextAsync(file.FullName, cancellation);
                public static implicit operator FileInfo(TempFile temp) => temp.Info;

                void IDisposable.Dispose()
                {
                    if (file.Exists)
                        file.Delete();
                }
            }
        }
    }
}
