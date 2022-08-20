namespace Joba.IBM.RPA
{
    public class WalPullService
    {
        public WalPullService(IRpaClient client, Project project, Environment environment)
        {
            One = new OneWal(client, project, environment);
            All = new AllWal(client, project, environment);
        }

        public OneWal One { get; }
        public AllWal All { get; }

        public class AllWal
        {
            private readonly Project project;
            private readonly Environment environment;
            private readonly IRpaClient client;

            public AllWal(IRpaClient client, Project project, Environment environment)
            {
                this.client = client;
                this.project = project;
                this.environment = environment;
            }

            public event EventHandler<ContinueOperationEventArgs>? ShouldContinueOperation;
            public event EventHandler<PullingEventArgs>? Pulling;
            public event EventHandler<PulledAllEventArgs>? Pulled;

            public async Task PullAsync(CancellationToken cancellation)
            {
                var args = new ContinueOperationEventArgs { Project = project, Environment = environment };
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
                    Pulling?.Invoke(this, new PullingEventArgs { Current = index + 1, Total = scripts.Length, Script = script, Project = project, Environment = environment });

                    var wal = environment.GetLocalWal(script.Name);
                    if (wal == null)
                        wal = await environment.CreateWalAsync(client.Script, script.Name, cancellation);
                    else
                        await wal.OverwriteToLatestAsync(client.Script, script.Name, cancellation);

                    wals.Add(wal);
                }

                Pulled?.Invoke(this, new PulledAllEventArgs { Files = wals, Project = project, Environment = environment });
            }
        }

        public class OneWal
        {
            private readonly Project project;
            private readonly Environment environment;
            private readonly IRpaClient client;

            public OneWal(IRpaClient client, Project project, Environment environment)
            {
                this.client = client;
                this.project = project;
                this.environment = environment;
            }

            public event EventHandler<ContinueOperationEventArgs>? ShouldContinueOperation;
            public event EventHandler<PulledOneEventArgs>? Pulled;

            public async Task PullAsync(string fileName, CancellationToken cancellation)
            {
                var wal = environment.GetLocalWal(fileName);
                if (wal == null)
                {
                    wal = await environment.CreateWalAsync(client.Script, fileName, cancellation);
                    Pulled?.Invoke(this, new PulledOneEventArgs { File = wal, NewFile = true, NewVersion = wal.Version!.Value, Project = project, Environment = environment });
                }
                else if (!wal.IsFromServer)
                {
                    var args = new ContinueOperationEventArgs { File = wal, Project = project, Environment = environment };
                    ShouldContinueOperation?.Invoke(this, args);
                    if (!args.Continue.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (args.Continue.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    await wal.OverwriteToLatestAsync(client.Script, fileName, cancellation);
                    Pulled?.Invoke(this, new PulledOneEventArgs { File = wal, NewVersion = wal.Version!.Value, Project = project, Environment = environment });
                }
                else
                {
                    var args = new ContinueOperationEventArgs { File = wal, Project = project, Environment = environment };
                    ShouldContinueOperation?.Invoke(this, args);
                    if (!args.Continue.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (args.Continue.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var previousVersion = wal.Version;
                    await wal.UpdateToLatestAsync(client.Script, cancellation);
                    Pulled?.Invoke(this, new PulledOneEventArgs { File = wal, PreviousVersion = previousVersion, NewVersion = wal.Version!.Value, Project = project, Environment = environment });
                }
            }
        }
    }

    public class ContinueOperationEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public WalFile? File { get; init; }
        public bool? Continue { get; set; }
    }

    public class PullingEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public required Script Script { get; init; }
        public required int Total { get; init; }
        public required int Current { get; init; }
    }

    public class PulledAllEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public required IEnumerable<WalFile> Files { get; init; }
    }

    public class PulledOneEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public required WalFile File { get; init; }
        public required int NewVersion { get; init; }
        public int? PreviousVersion { get; init; }
        public bool NewFile { get; init; }
    }
}