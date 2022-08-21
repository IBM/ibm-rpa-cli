namespace Joba.IBM.RPA
{
    public class WalPullService
    {
        public WalPullService(IRpaClient client, Project project, Environment environment)
        {
            One = new PullOne(client, project, environment);
            All = new PullMany(client, project, environment);
        }

        public IPullOne<WalFile> One { get; }
        public IPullMany All { get; }

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

                var scripts = (await client.Script.SearchAsync(project.Name, 50, cancellation)).Where(s => s.Name.StartsWith(project.Name)).ToArray();
                var wals = new List<WalFile>(scripts.Length);
                for (var index = 0; index < scripts.Length; index++)
                {
                    var script = scripts[index];
                    Pulling?.Invoke(this, new PullingEventArgs { Current = index + 1, Total = scripts.Length, ResourceName = script.Name, Project = project, Environment = environment });

                    var wal = environment.GetLocalWal(script.Name);
                    if (wal == null)
                        wal = await environment.CreateWalAsync(client.Script, script.Name, cancellation);
                    else
                        await wal.OverwriteToLatestAsync(client.Script, script.Name, cancellation);

                    wals.Add(wal);
                }

                Pulled?.Invoke(this, new PulledAllEventArgs { Total = wals.Count, Project = project, Environment = environment });
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
                var wal = environment.GetLocalWal(name);
                if (wal == null)
                {
                    wal = await environment.CreateWalAsync(client.Script, name, cancellation);
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