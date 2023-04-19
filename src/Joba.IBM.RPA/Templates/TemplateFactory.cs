using System.Reflection;

namespace Joba.IBM.RPA
{
    public class TemplateFactory
    {
        private readonly DirectoryInfo workingDir;
        private readonly Assembly assembly;

        public TemplateFactory(DirectoryInfo workingDir, Assembly assembly)
        {
            this.workingDir = workingDir;
            this.assembly = assembly;
        }

        //TODO: templates should be json files with metadata about them, e.g, 'excel' template would be 'unattended' bot
        public async Task<WalFile> CreateAsync(WalFileName fileName, string templateName, CancellationToken cancellation)
        {
            if (!templateName.EndsWith(WalFile.Extension))
                templateName += WalFile.Extension;

            //TODO: do not embbed into the assembly, but publish from the 'templates' folder
            var templateStream = assembly.GetManifestResourceStream(@$"Joba.IBM.RPA.Cli.Templates.{templateName}");
            if (templateStream == null)
                throw new NotSupportedException($"Template '{templateName}' does not exist");

            var file = new FileInfo(Path.Combine(workingDir.FullName, fileName));
            if (file.Exists)
                throw new TemplateException($"Cannot create the file '{file.FullName}' because it already exists");

            using (var fileStream = File.OpenWrite(file.FullName))
            {
                templateStream.Position = 0;
                await templateStream.CopyToAsync(fileStream, cancellation);
            }

            file.Refresh();
            var wal = WalFile.Read(file);
            var workingDirectoryEscaped = workingDir.FullName.Replace("\\", "\\\\"); //NOTE: wal expects '\\' in directory separators.
            var content = new WalContent(wal.Content.ToString().Replace("@{workingDirectory}", workingDirectoryEscaped));
            wal.Overwrite(content);
            return wal;
        }
    }
}