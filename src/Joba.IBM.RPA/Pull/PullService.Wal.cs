using Joba.IBM.RPA.Server;

namespace Joba.IBM.RPA
{
    public class WalPullService
    {
        public WalPullService(IRpaClient client, IProject project, string alias)
        {
            One = new PullOne(client, project, alias);
            Many = new PullMany(client, project, alias);
        }

        public IPullOne<WalFile> One { get; }
        public IPullMany Many { get; }

        class PullMany : IPullMany
        {
            private readonly IProject project;
            private readonly string alias;
            private readonly IRpaClient client;

            internal PullMany(IRpaClient client, IProject project, string alias)
            {
                this.client = client;
                this.project = project;
                this.alias = alias;
            }

            public event EventHandler<ContinueOperationEventArgs>? ShouldContinueOperation;
            public event EventHandler<PullingEventArgs>? Pulling;
            public event EventHandler<PulledAllEventArgs>? Pulled;

            public async Task PullAsync(NamePattern pattern, CancellationToken cancellation)
            {
                var args = new ContinueOperationEventArgs { Project = project, Alias = alias, Pattern = pattern };
                ShouldContinueOperation?.Invoke(this, args);
                if (!args.Continue.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (args.Continue.Value == false)
                    throw new OperationCanceledException("User cancelled the operation");

                Pulling?.Invoke(this, new PullingEventArgs { Project = project, Alias = alias });
                var task = pattern.HasWildcard ? PullWildcardAsync(pattern, cancellation) : PullFixedAsync(pattern.Name, cancellation);
                var scripts = (await task).ToArray();

                var wals = new List<WalFile>(scripts.Length);
                for (var index = 0; index < scripts.Length; index++)
                {
                    var script = scripts[index];
                    Pulling?.Invoke(this, new PullingEventArgs { Current = index + 1, Total = scripts.Length, ResourceName = script.Name, Project = project, Alias = alias });

                    var wal = project.Scripts.Get(script.Name);
                    if (wal == null)
                        wal = await project.Scripts.DownloadLatestAsync(client.Script, script.Name, cancellation);
                    else
                        await wal.OverwriteToLatestAsync(client.Script, script.Name, cancellation);

                    wals.Add(wal);
                }

                Pulled?.Invoke(this, new PulledAllEventArgs { Total = wals.Count, Project = project, Alias = alias, Pattern = pattern });
            }

            private async Task<IEnumerable<Script>> PullWildcardAsync(NamePattern pattern, CancellationToken cancellation) =>
                await client.Script.SearchAsync(pattern.Name, 50, cancellation)
                        .ContinueWith(c => c.Result.Where(s => pattern.Matches(s.Name)), TaskContinuationOptions.OnlyOnRanToCompletion);

            private async Task<IEnumerable<Script>> PullFixedAsync(string scriptName, CancellationToken cancellation)
            {
                var script = await client.Script.SearchAsync(scriptName, 5, cancellation)
                         .ContinueWith(c => c.Result.FirstOrDefault(s => s.Name == scriptName), TaskContinuationOptions.OnlyOnRanToCompletion)
                         .ContinueWith(s => s.Result ?? throw new Exception($"Could not find script '{scriptName}'"), TaskContinuationOptions.OnlyOnRanToCompletion);

                return new Script[] { script };
            }
        }

        class PullOne : IPullOne<WalFile>
        {
            private readonly IProject project;
            private readonly string alias;
            private readonly IRpaClient client;

            internal PullOne(IRpaClient client, IProject project, string alias)
            {
                this.client = client;
                this.project = project;
                this.alias = alias;
            }

            public event EventHandler<ContinueOperationEventArgs<WalFile>>? ShouldContinueOperation;
            public event EventHandler<PulledOneEventArgs<WalFile>>? Pulled;

            public async Task PullAsync(string name, CancellationToken cancellation)
            {
                var wal = project.Scripts.Get(name);
                if (wal == null)
                {
                    wal = await project.Scripts.DownloadLatestAsync(client.Script, name, cancellation);
                    Pulled?.Invoke(this, PulledOneEventArgs<WalFile>.Created(alias, project, wal));
                }
                else if (!wal.IsFromServer)
                {
                    var args = new ContinueOperationEventArgs<WalFile> { Resource = wal, Project = project, Alias = alias, Pattern = new NamePattern(name) };
                    ShouldContinueOperation?.Invoke(this, args);
                    if (!args.Continue.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (args.Continue.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var previous = wal.Clone();
                    await wal.OverwriteToLatestAsync(client.Script, name, cancellation);
                    Pulled?.Invoke(this, PulledOneEventArgs<WalFile>.Updated(alias, project, wal, previous));
                }
                else
                {
                    var args = new ContinueOperationEventArgs<WalFile> { Resource = wal, Project = project, Alias = alias, Pattern = new NamePattern(name) };
                    ShouldContinueOperation?.Invoke(this, args);
                    if (!args.Continue.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (args.Continue.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var previous = wal.Clone();
                    await wal.UpdateToLatestAsync(client.Script, cancellation);

                    PulledOneEventArgs<WalFile>? pulledArgs;
                    if (previous.Version == wal.Version)
                        pulledArgs = PulledOneEventArgs<WalFile>.NoChange(alias, project, wal);
                    else
                        pulledArgs = PulledOneEventArgs<WalFile>.Updated(alias, project, wal, previous);
                    Pulled?.Invoke(this, pulledArgs);
                }
            }
        }
    }
}