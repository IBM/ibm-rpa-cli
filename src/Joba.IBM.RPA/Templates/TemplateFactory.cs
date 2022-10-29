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

        public async Task<WalFile?> CreateAsync(WalFileName fileName, string templateName, CancellationToken cancellation)
        {
            if (!templateName.EndsWith(WalFile.Extension))
                templateName += WalFile.Extension;

            //TODO: do not embbed into the assembly, but publish from the 'templates' folder
            var templateStream = assembly.GetManifestResourceStream(@$"Joba.IBM.RPA.Cli.Templates.{templateName}");
            if (templateStream == null)
                return null;

            var file = new FileInfo(Path.Combine(workingDir.FullName, fileName));
            if (file.Exists)
                return null;

            using (var fileStream = File.OpenWrite(file.FullName))
            {
                templateStream.Position = 0;
                await templateStream.CopyToAsync(fileStream, cancellation);
            }

            //TODO: discover which RPA version is installed and use that in the WAL metadata (ProductVersion)
            return WalFile.Read(file);
        }
    }
}