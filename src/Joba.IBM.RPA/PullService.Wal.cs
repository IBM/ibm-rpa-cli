namespace Joba.IBM.RPA
{
    public class WalPullService
    {
        public WalPullService(IRpaClient client, Project project, Environment environment)
        {
            One = new PullOne(client, project, environment);
            Many = new PullMany(client, project, environment);
        }

        public IPullOne<WalFile> One { get; }
        public IPullMany Many { get; }

        class PullMany : IPullMany
        {
            private readonly Project project;
            private readonly Environment environment;
            private readonly IRpaClient client;

            internal PullMany(IRpaClient client, Project project, Environment environment)
            {
                this.client = client;
                this.project = project;
                this.environment = environment;
            }

            public event EventHandler<ContinuePullOperationEventArgs>? ShouldContinueOperation;
            public event EventHandler<PullingEventArgs>? Pulling;
            public event EventHandler<PulledAllEventArgs>? Pulled;

            public async Task PullAsync(CancellationToken cancellation)
            {
                var args = new ContinuePullOperationEventArgs { Project = project, Environment = environment };
                ShouldContinueOperation?.Invoke(this, args);
                if (!args.Continue.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (args.Continue.Value == false)
                    throw new OperationCanceledException("User cancelled the operation");

                Pulling?.Invoke(this, new PullingEventArgs { Project = project, Environment = environment });
                var fromWildcard = await PullWildcardAsync(cancellation);
                var fromFixed = await PullFixedAsync(cancellation);
                var scripts = fromWildcard.Concat(fromFixed).ToArray();

                var wals = new List<WalFile>(scripts.Length);
                for (var index = 0; index < scripts.Length; index++)
                {
                    var script = scripts[index];
                    Pulling?.Invoke(this, new PullingEventArgs { Current = index + 1, Total = scripts.Length, ResourceName = script.Name, Project = project, Environment = environment });

                    var wal = environment.Files.Get(script.Name);
                    if (wal == null)
                        wal = await environment.Files.DownloadLatestAsync(client.Script, script.Name, cancellation);
                    else
                        await wal.OverwriteToLatestAsync(client.Script, script.Name, cancellation);

                    wals.Add(wal);
                }

                Pulled?.Invoke(this, new PulledAllEventArgs { Total = wals.Count, Project = project, Environment = environment });
            }

            private async Task<IEnumerable<Script>> PullWildcardAsync(CancellationToken cancellation)
            {
                var wildcard = project.Files.GetWildcards();
                var tasks = wildcard
                    .Select(p => client.Script.SearchAsync(p.Name, 50, cancellation)
                        .ContinueWith(c => c.Result.Where(s => p.Matches(s.Name)), TaskContinuationOptions.OnlyOnRanToCompletion))
                    .ToList();

                var items = await Task.WhenAll(tasks);
                return items.SelectMany(p => p).ToList();
            }

            private async Task<IEnumerable<Script>> PullFixedAsync(CancellationToken cancellation)
            {
                var files = project.Files.GetFixed();
                var tasks = files
                    .Select(f => client.Script.SearchAsync(f, 5, cancellation)
                        .ContinueWith(c => c.Result.FirstOrDefault(s => s.Name == f), TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith(s => s.Result ?? throw new Exception($"Could not find script '{f}'")))
                    .ToList();

                return await Task.WhenAll(tasks);
            }
        }

        class PullOne : IPullOne<WalFile>
        {
            private readonly Project project;
            private readonly Environment environment;
            private readonly IRpaClient client;

            internal PullOne(IRpaClient client, Project project, Environment environment)
            {
                this.client = client;
                this.project = project;
                this.environment = environment;
            }

            public event EventHandler<ContinuePullOperationEventArgs<WalFile>>? ShouldContinueOperation;
            public event EventHandler<PulledOneEventArgs<WalFile>>? Pulled;

            public async Task PullAsync(string name, CancellationToken cancellation)
            {
                var wal = environment.Files.Get(name);
                if (wal == null)
                {
                    wal = await environment.Files.DownloadLatestAsync(client.Script, name, cancellation);
                    project.Files.Add(new NamePattern(name));

                    Pulled?.Invoke(this, PulledOneEventArgs<WalFile>.Created(environment, project, wal));
                }
                else if (!wal.IsFromServer)
                {
                    var args = new ContinuePullOperationEventArgs<WalFile> { Resource = wal, Project = project, Environment = environment };
                    ShouldContinueOperation?.Invoke(this, args);
                    if (!args.Continue.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (args.Continue.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var previous = wal.Clone();
                    await wal.OverwriteToLatestAsync(client.Script, name, cancellation);
                    project.Files.Add(new NamePattern(name));
                    
                    Pulled?.Invoke(this, PulledOneEventArgs<WalFile>.Updated(environment, project, wal, previous));
                }
                else
                {
                    var args = new ContinuePullOperationEventArgs<WalFile> { Resource = wal, Project = project, Environment = environment };
                    ShouldContinueOperation?.Invoke(this, args);
                    if (!args.Continue.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (args.Continue.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var previous = wal.Clone();
                    await wal.UpdateToLatestAsync(client.Script, cancellation);
                    project.Files.Add(new NamePattern(name));

                    PulledOneEventArgs<WalFile>? pulledArgs;
                    if (previous.Version == wal.Version)
                        pulledArgs = PulledOneEventArgs<WalFile>.NoChange(environment, project, wal);
                    else
                        pulledArgs = PulledOneEventArgs<WalFile>.Updated(environment, project, wal, previous);
                    Pulled?.Invoke(this, pulledArgs);
                }
            }
        }
    }
}