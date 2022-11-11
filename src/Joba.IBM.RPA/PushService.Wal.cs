namespace Joba.IBM.RPA
{
    public class WalPushService
    {
        public WalPushService(IRpaClient client, IProject project, string alias)
        {
            One = new PushOne(client, project, alias);
            //Many = new PullMany(client, project, environment);
        }

        public IPushOne<WalFile> One { get; }

        class PushOne : IPushOne<WalFile>
        {
            private readonly IProject project;
            private readonly string alias;
            private readonly IRpaClient client;

            internal PushOne(IRpaClient client, IProject project, string alias)
            {
                this.client = client;
                this.project = project;
                this.alias = alias;
            }

            public event EventHandler<ContinueOperationEventArgs<WalFile>>? ShouldContinueOperation;
            public event EventHandler<PushedOneEventArgs<WalFile>>? Pushed;

            public async Task PushAsync(string name, CancellationToken cancellation)
            {
                var wal = project.Scripts.Get(name);
                if (wal == null)
                    throw new Exception($"Could not push '{name}' because it doesn't exist.");

                var args = new ContinueOperationEventArgs<WalFile> { Resource = wal, Project = project, Alias = alias, Pattern = new NamePattern(name) };
                ShouldContinueOperation?.Invoke(this, args);
                if (!args.Continue.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (args.Continue.Value == false)
                    throw new OperationCanceledException("User cancelled the operation");

                var model = wal.PrepareToPublish($"New version from {project.Name} project");
                var version = await client.Script.PublishAsync(model, cancellation);
                wal.Overwrite(version);

                Pushed?.Invoke(this, new PushedOneEventArgs<WalFile> { Resource = wal, Project = project, EnvironmentAlias = alias });
            }
        }
    }
}