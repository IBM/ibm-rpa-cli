using Joba.Pipeline;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace Joba.IBM.RPA
{

    public sealed partial class Bluewasher
    {
        class InjectDependencies : DefaultPipelineMiddleware<BuildRobotContext>
        {
            private readonly ILogger logger;
            private readonly ISnippetFactory snippetFactory;

            public InjectDependencies(ILogger logger, ISnippetFactory snippetFactory)
            {
                this.logger = logger;
                this.snippetFactory = snippetFactory;
            }

            protected override async Task Run(BuildRobotContext context, CancellationToken cancellation)
            {
                if (!context.Dependencies.Any())
                    return;
                if (context.File == null)
                    throw new InvalidOperationException($"Cannot inject dependencies because the file '{context.OriginalFile.Name}' was not copied to the output '{context.OutputDirectory.FullName}'.");
                if (context.Scanner == null)
                    throw new InvalidOperationException($"Cannot inject dependencies because the scanner was not created.");

                ApplyDefineVariableSnippetIfNeeded(context);
                await ApplyDependenciesSnippet(context, cancellation);
            }

            private async Task ApplyDependenciesSnippet(BuildRobotContext context, CancellationToken cancellation)
            {
                var dependenciesZipPath = CreateZip(context);
                var snippet = await snippetFactory.GetAsync("InjectDependencies", cancellation) ?? throw new InvalidOperationException($"Could not find snippet 'InjectDependencies'");
                snippet.Configure("zipFileAsBase64", Convert.ToBase64String(File.ReadAllBytes(dependenciesZipPath)));
                snippet.Apply(context.File!);
                //TODO: delete zip file
            }

            private static void ApplyDefineVariableSnippetIfNeeded(BuildRobotContext context)
            {
                var analyzer = context.Scanner!.GetAnalyzerFromCache(context.File!);
                var variables = analyzer.EnumerateCommands<DefineVariableLine>(DefineVariableLine.Verb);
                var workingDirVar = variables.FirstOrDefault(v => v.Name == "workingDirectory");
                if (workingDirVar != null && workingDirVar.Type != "String")
                    throw new InvalidOperationException($"Although the variable 'workingDirectory' is defined in '{context.File!.Name}', it must be 'String' type and not '{workingDirVar.Type}' type");
                if (workingDirVar == null)
                {
                    var defVarSnippet = (ISnippet)new Snippet("defVar --name workingDirectory --type String", null, null);
                    defVarSnippet.Apply(context.File!);
                }
            }

            private string CreateZip(BuildRobotContext context)
            {
                var dependenciesZipPath = Path.Combine(context.OutputDirectory.FullName, $"{context.File!.Name.WithoutExtension}.zip");
                logger.LogDebug("Creating the dependencies zip file '{FilePath}'", dependenciesZipPath);
                using (var fileStream = new FileStream(dependenciesZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true) { Comment = $"Robot {context.File!.Name} dependencies of project {context.Project.Name}" };
                    foreach (var dependency in context.Dependencies)
                    {
                        var relativePath = Path.GetRelativePath(context.Project.WorkingDirectory.FullName, dependency.FullName);
                        _ = archive.CreateEntryFromFile(dependency.FullName, relativePath);
                        logger.LogDebug("Entry '{Entry}' added to '{FilePath}'", relativePath, dependenciesZipPath);
                    }
                }

                return dependenciesZipPath;
            }
        }
    }
}
