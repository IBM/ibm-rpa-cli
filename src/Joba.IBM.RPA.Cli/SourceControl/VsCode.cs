using System.Diagnostics;

namespace Joba.IBM.RPA.Cli
{
    internal class VsCode
    {
        private const string ExeName = "code";

        /// <summary>
        /// Launches a new session of VSCode to 3-way merge files, according to the <a href="https://code.visualstudio.com/docs/editor/command-line#_core-cli-options">documentation</a>.
        /// </summary>
        internal async Task MergeAsync(FileInfo leftFile, FileInfo rightFile, FileInfo baseFile, FileInfo resultFile, CancellationToken cancellation)
        {
            var arguments = $"-n -m \"{leftFile.FullName}\" \"{rightFile.FullName}\" \"{baseFile.FullName}\" \"{resultFile.FullName}\" --wait";
            var info = new ProcessStartInfo(ExeName, arguments) { UseShellExecute = true, CreateNoWindow = true };
            using var process = Process.Start(info);
            if (process == null)
                throw new Exception($"Could not start '{ExeName}' tool.");

            await process.WaitForExitAsync(cancellation);
        }

        /// <summary>
        /// Launches a new session of VSCode to show differences between two files, according to the <a href="https://code.visualstudio.com/docs/editor/command-line#_core-cli-options">documentation</a>.
        /// </summary>
        internal async Task DiffAsync(FileInfo leftFile, FileInfo rightFile, CancellationToken cancellation)
        {
            var arguments = $"-n -d \"{leftFile.FullName}\" \"{rightFile.FullName}\" --wait";
            var info = new ProcessStartInfo(ExeName, arguments) { UseShellExecute = true, CreateNoWindow = true };
            using var process = Process.Start(info);
            if (process == null)
                throw new Exception($"Could not start '{ExeName}' tool.");

            await process.WaitForExitAsync(cancellation);
        }
    }
}
