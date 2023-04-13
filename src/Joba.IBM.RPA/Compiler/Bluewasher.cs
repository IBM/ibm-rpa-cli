using Joba.Pipeline;
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
    public sealed partial class Bluewasher : ICompiler
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
                logger.LogInformation("Build started for project '{Project}'", project.Name);
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

        private async Task<BuildResult> BuildRobotAsync(IProject project, Robot robot, DirectoryInfo outputDirectory, CancellationToken cancellation)
        {
            //TODO: restore packages before building...
            var context = new BuildRobotContext(project, robot, outputDirectory);
            var pipeline = AsyncPipeline<BuildRobotContext>.Create()
                .Add(new CopyOriginalFile(logger))
                .Add(new ScanReferences(logger))
                .Add(new InjectDependencies(logger, snippetFactory))
                .Catch((ctx, source, c) =>
                {
                    ctx.SetError(source.Exception);
                    source.MarkAsHandled();
                    return Task.CompletedTask;
                })
                .Finally((ctx, c) =>
                {
                    ctx.Stop();
                    return Task.CompletedTask;
                });

            logger.LogInformation("Build started for: {File}", context.OriginalFile.Info.FullName);
            await pipeline.ExecuteAsync(context, cancellation);

            if (context.Error == null)
                return BuildResult.Succeed(context.ElapsedTime, robot, context.File!);

            return BuildResult.Failed(context.ElapsedTime, context.Error);
        }
        
        class BuildRobotContext
        {
            private readonly Stopwatch stopwatch;

            internal BuildRobotContext(IProject project, Robot robot, DirectoryInfo outputDirectory)
            {
                stopwatch = Stopwatch.StartNew();
                OriginalFile = project.Scripts.Get(robot.Name) ?? throw new InvalidOperationException($"Could not find the wal file for the robot named '{robot.Name}'");
                Project = project;
                Robot = robot;
                OutputDirectory = outputDirectory;
            }

            internal IProject Project { get; }
            internal Robot Robot { get; }
            internal WalFile OriginalFile { get; }
            internal DirectoryInfo OutputDirectory { get; }
            internal TimeSpan ElapsedTime { get; }
            internal Exception? Error { get; private set; }
            internal WalFile? File { get; set; }
            internal ReferenceScanner? Scanner { get; private set; }
            internal IEnumerable<FileInfo> Dependencies { get; set; } = Enumerable.Empty<FileInfo>();

            internal ReferenceScanner CreateScanner(ILogger logger)
            {
                Scanner = new ReferenceScanner(logger, Project, File!);
                return Scanner;
            }
            internal void Stop() => stopwatch.Stop();
            internal void SetError(Exception exception) => Error = exception;
        }

        class ReferenceScanner
        {
            private readonly ILogger logger;
            private readonly IProject project;
            private readonly IDictionary<string, WalAnalyzer> cache = new Dictionary<string, WalAnalyzer>();
            private WalFile current;
            private int logPadding = 0;

            internal ReferenceScanner(ILogger logger, IProject project, WalFile wal)
            {
                this.logger = logger;
                this.project = project;
                this.project = project;
                current = wal;
            }

            internal WalAnalyzer GetAnalyzerFromCache(WalFile wal)
            {
                if (!cache.TryGetValue(wal.Info.FullName, out var analyzer))
                    throw new InvalidOperationException($"Could not find a parsed analyzer in cache for '{wal.Info.FullName}'");

                return analyzer;
            }

            private WalAnalyzer CreateAnalyzer()
            {
                if (cache.TryGetValue(current.Info.FullName, out var analyzer))
                    return analyzer;

                analyzer = new WalAnalyzer(current);
                cache.Add(current.Info.FullName, analyzer);
                return analyzer;
            }

            private ReferenceScanner Next(WalFile wal)
            {
                ++logPadding;
                current = wal;
                return this;
            }

            private void LogDebug(string? message, params object?[] args)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug(new string(' ', logPadding) + message, args);
            }

            private void LogWarning(string? message, params object?[] args)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning(new string(' ', logPadding) + message, args);
            }

            internal IEnumerable<Reference> Scan() => ScanRecursively(this);

            private static IEnumerable<Reference> ScanRecursively(ReferenceScanner context)
            {
                context.LogDebug("Analyzing '{Script}' ({File})", context.current.Name, context.current.Info.FullName);
                var analyzer = context.CreateAnalyzer();

                context.LogDebug("Searching '{command}' command references on '{Script}'", ExecuteScriptLine.Verb, context.current.Name);
                var executeScripts = analyzer.EnumerateCommands<ExecuteScriptLine>(ExecuteScriptLine.Verb);
                foreach (var executeScript in executeScripts)
                {
                    var relativePath = executeScript.GetRelativePath(context.project.WorkingDirectory);
                    if (relativePath == null)
                    {
                        context.LogWarning("Skipping the reference on line '{Line}' of '{Script}' because the '--name' parameter does not follow the format '${[working_directory_variable]}\\[path_of_the_wal_file_within_working_directory]'.", executeScript.LineNumber, context.current.Name, executeScript.Name);
                        context.LogWarning("Examples:\n" +
                            "  ${workingDir}\\myscript.wal\n" +
                            "  ${var1}\\packages\\package1.wal");
                        continue;
                    }

                    context.LogDebug("Reference found on line '{Line}' of '{Script}': {RelativePath}", executeScript.LineNumber, context.current.Name, relativePath);

                    var file = new FileInfo(Path.Combine(context.project.WorkingDirectory.FullName, relativePath));
                    var wal = context.project.Scripts.Get(file);
                    yield return wal == null
                        ? throw new FileNotFoundException($"The file '{file.FullName}' could not be found.", file.Name)
                        : new Reference(context.current.Info, wal.Info);

                    foreach (var reference in ScanRecursively(context.Next(wal)))
                        yield return reference;
                }
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