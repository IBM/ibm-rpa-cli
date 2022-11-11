using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace Joba.IBM.RPA
{
    public interface ICompiler
    {
        Task<BuildResult> BuildAsync(BuildArguments arguments, CancellationToken cancellation);
    }

    /// <summary>
    /// The IBM RPA compiler. Codename "<see cref="Bluewasher"/>".
    /// </summary>
    public sealed class Bluewasher : ICompiler
    {
        private readonly ILogger logger;
        private readonly ISnippetFactory snippetFactory;

        public Bluewasher(ILogger logger)
        {
            this.logger = logger;
            snippetFactory = new CurrentAssemblySnippetFactory();
        }

        async Task<BuildResult> ICompiler.BuildAsync(BuildArguments arguments, CancellationToken cancellation)
        {
            if (arguments.Robot != null)
                return await BuildRobotAsync(arguments.Project, arguments.Robot.Value, arguments.OutputDirectory, cancellation);

            return await BuildRobotAsync(arguments.Project, arguments.OutputDirectory, cancellation);
        }

        private async Task<BuildResult> BuildRobotAsync(IProject project, DirectoryInfo outputDirectory, CancellationToken cancellation)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var tasks = project.Robots.Select(robot => BuildRobotAsync(project, robot, outputDirectory, cancellation)).ToArray();
                var results = await WhenAllFailFast(tasks, cancellation);
                stopwatch.Stop();

                var robots = results.SelectMany(r => r.Robots);
                return BuildResult.Succeed(stopwatch.Elapsed, new Dictionary<Robot, WalFile>(robots));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BuildResult.Failed(stopwatch.Elapsed, ex);
            }
        }

        /// <summary>
        /// Fails the <see cref="Task.WhenAll(Task[])"/> when one of the them fails.
        /// From https://stackoverflow.com/a/69338551/1830639
        /// </summary>
        private static Task<TResult[]> WhenAllFailFast<TResult>(Task<TResult>[] tasks, CancellationToken cancellation)
        {
            ArgumentNullException.ThrowIfNull(tasks);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            Task<TResult>? failedTask = null;
            var flags = TaskContinuationOptions.DenyChildAttach |
                TaskContinuationOptions.ExecuteSynchronously;
            Action<Task<TResult>> continuationAction = new(task =>
            {
                if (!task.IsCompletedSuccessfully)
                    if (Interlocked.CompareExchange(ref failedTask, task, null) is null)
                        cts.Cancel();
            });
            var continuations = tasks.Select(task => task
                .ContinueWith(continuationAction, cts.Token, flags, TaskScheduler.Default));

            return Task.WhenAll(continuations).ContinueWith(allContinuations =>
            {
                cts.Dispose();
                var localFailedTask = Volatile.Read(ref failedTask);
                if (localFailedTask is not null)
                    return Task.WhenAll(localFailedTask);
                // At this point all the tasks are completed successfully
                Debug.Assert(tasks.All(t => t.IsCompletedSuccessfully));
                Debug.Assert(allContinuations.IsCompletedSuccessfully);
                return Task.WhenAll(tasks);
            }, default, flags, TaskScheduler.Default).Unwrap();
        }

        //TODO: refactor to use Pipeline
        private async Task<BuildResult> BuildRobotAsync(IProject project, Robot robot, DirectoryInfo outputDirectory, CancellationToken cancellation)
        {
            var file = project.Scripts.Get(robot.Name) ?? throw new InvalidOperationException($"Could not find the wal file for the robot named '{robot.Name}'");
            var stopwatch = Stopwatch.StartNew();

            //TODO: restore packages before building...
            logger.LogInformation("Build started for: {File}", file.Info.FullName);
            try
            {
                var copyPath = Path.Combine(outputDirectory.FullName, file.Info.Name);
                logger.LogDebug("Copying {Source} to {Target}", file.Info.FullName, copyPath);
                File.Copy(file.Info.FullName, copyPath, true);
                var fileCopy = WalFile.Read(new FileInfo(copyPath));

                var context = new ScanningContext(logger, project, fileCopy);
                var references = ScanReferencesRecursively(context).ToList();
                var dependencies = references.Select(r => r.Right).Distinct().ToList();
                logger.LogInformation("Total of dependencies found for '{File}': {Total}", fileCopy.Name, dependencies.Count);

                if (dependencies.Any())
                {
                    var dependenciesZipPath = Path.Combine(outputDirectory.FullName, $"{fileCopy.Name.WithoutExtension}.zip");
                    logger.LogDebug("Creating the dependencies zip file '{FilePath}'", dependenciesZipPath);
                    using (var fileStream = new FileStream(dependenciesZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true) { Comment = $"Robot {fileCopy.Name} dependencies of project {project.Name}" };
                        foreach (var dependency in dependencies)
                        {
                            var relativePath = Path.GetRelativePath(project.WorkingDirectory.FullName, dependency.FullName);
                            var entry = archive.CreateEntryFromFile(dependency.FullName, relativePath);
                            logger.LogDebug("Entry '{Entry}' added to '{FilePath}'", relativePath, dependenciesZipPath);
                        }
                    }

                    var snippet = await snippetFactory.GetAsync("InjectDependencies", cancellation) ?? throw new InvalidOperationException($"Could not find snippet 'InjectDependencies'");
                    snippet.Configure("zipFileAsBase64", Convert.ToBase64String(File.ReadAllBytes(dependenciesZipPath)));
                    //TODO: delete zip file

                    var analyzer = context.GetAnalyzerFromCache(fileCopy);
                    var variables = analyzer.EnumerateCommands<DefineVariableLine>(DefineVariableLine.Verb);
                    var workingDirVar = variables.FirstOrDefault(v => v.Name == "workingDirectory");
                    if (workingDirVar != null && workingDirVar.Type != "String")
                        throw new InvalidOperationException($"Although the variable 'workingDirectory' is defined in '{fileCopy.Name}', it must be 'String' type and not '{workingDirVar.Type}' type");
                    if (workingDirVar == null)
                    {
                        var defVarSnippet = (ISnippet)new Snippet("defVar --name workingDirectory --type String", null, null);
                        defVarSnippet.Apply(fileCopy);
                    }

                    snippet.Apply(fileCopy);
                }

                stopwatch.Stop();
                return BuildResult.Succeed(stopwatch.Elapsed, robot, fileCopy);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return BuildResult.Failed(stopwatch.Elapsed, ex);
            }
        }

        private IEnumerable<Reference> ScanReferencesRecursively(ScanningContext context)
        {
            var project = context.Project;
            var current = context.Current;

            context.LogDebug("Analyzing '{Script}' ({File})", current.Name, current.Info.FullName);
            var analyzer = context.CreateAnalyzer();

            context.LogDebug("Searching '{command}' command references on '{Script}'", ExecuteScriptLine.Verb, current.Name);
            var executeScripts = analyzer.EnumerateCommands<ExecuteScriptLine>(ExecuteScriptLine.Verb);
            foreach (var executeScript in executeScripts)
            {
                var relativePath = executeScript.GetRelativePath(project.WorkingDirectory);
                if (relativePath == null)
                {
                    context.LogWarning("Skipping the reference on line '{Line}' of '{Script}' because the '--name' parameter does not follow the format '${[working_directory_variable]}\\[path_of_the_wal_file_within_working_directory]'.", executeScript.LineNumber, current.Name, executeScript.Name);
                    context.LogWarning("Examples:\n" +
                        "  ${workingDir}\\myscript.wal\n" +
                        "  ${var1}\\packages\\package1.wal");
                    continue;
                }

                context.LogDebug("Reference found on line '{Line}' of '{Script}': {RelativePath}", executeScript.LineNumber, current.Name, relativePath);

                var file = new FileInfo(Path.Combine(project.WorkingDirectory.FullName, relativePath));
                var wal = project.Scripts.Get(file);
                yield return wal == null
                    ? throw new FileNotFoundException($"The file '{file.FullName}' could not be found.", file.Name)
                    : new Reference(current.Info, wal.Info);

                foreach (var reference in ScanReferencesRecursively(context.Next(wal)))
                    yield return reference;
            }
        }

        class ScanningContext
        {
            private readonly ILogger logger;
            private int logPadding = 0;
            private readonly IDictionary<string, WalAnalyzer> cache = new Dictionary<string, WalAnalyzer>();

            internal ScanningContext(ILogger logger, IProject project, WalFile wal)
            {
                this.logger = logger;
                Project = project;
                Current = wal;
            }

            internal IProject Project { get; }
            internal WalFile Current { get; private set; }

            internal WalAnalyzer CreateAnalyzer()
            {
                if (cache.TryGetValue(Current.Info.FullName, out var analyzer))
                    return analyzer;

                analyzer = new WalAnalyzer(Current);
                cache.Add(Current.Info.FullName, analyzer);
                return analyzer;
            }

            internal WalAnalyzer GetAnalyzerFromCache(WalFile wal)
            {
                if (!cache.TryGetValue(wal.Info.FullName, out var analyzer))
                    throw new InvalidOperationException($"Could not find a parsed analyzer in cache for '{wal.Info.FullName}'");

                return analyzer;
            }

            internal ScanningContext Next(WalFile wal)
            {
                ++logPadding;
                Current = wal;
                return this;
            }

            internal void LogDebug(string? message, params object?[] args)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug(new string(' ', logPadding) + message, args);
            }

            internal void LogWarning(string? message, params object?[] args)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning(new string(' ', logPadding) + message, args);
            }
        }

        class Reference
        {
            internal Reference(FileInfo left, FileInfo right)
            {
                Left = left;
                Right = right;
            }

            public FileInfo Left { get; }
            public FileInfo Right { get; }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                if (obj is null) return false;
                if (obj is Reference reference)
                    return reference.Left.FullName == Left.FullName && reference.Right.FullName == Right.FullName;
                return false;
            }
            public override int GetHashCode() => Left.FullName.GetHashCode() ^ Right.FullName.GetHashCode();
            public override string ToString() => $"{Left.Name} references {Right.Name}";
            public static bool operator ==(Reference? left, Reference? right) => left.Equals(right);
            public static bool operator !=(Reference? left, Reference? right) => !(left == right);
        }
    }
}
