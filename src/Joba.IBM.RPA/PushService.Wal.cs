namespace Joba.IBM.RPA
{
    public class WalPushService
    {
        public WalPushService(IRpaClient client, Project project, Environment environment)
        {
            One = new PushOne(client, project, environment);
            //Many = new PullMany(client, project, environment);
        }

        public IPushOne<WalFile> One { get; }

        class PushOne : IPushOne<WalFile>
        {
            private readonly Project project;
            private readonly Environment environment;
            private readonly IRpaClient client;

            internal PushOne(IRpaClient client, Project project, Environment environment)
            {
                this.client = client;
                this.project = project;
                this.environment = environment;
            }

            public event EventHandler<ContinueOperationEventArgs<WalFile>>? ShouldContinueOperation;
            public event EventHandler<PushedOneEventArgs<WalFile>>? Pushed;

            public async Task PushAsync(string name, CancellationToken cancellation)
            {
                var wal = environment.Files.Get(name);
                if (wal == null)
                    throw new Exception($"Could not push '{name}' because it doesn't exist.");

                var args = new ContinueOperationEventArgs<WalFile> { Resource = wal, Project = project, Environment = environment };
                ShouldContinueOperation?.Invoke(this, args);
                if (!args.Continue.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (args.Continue.Value == false)
                    throw new OperationCanceledException("User cancelled the operation");

                var model = wal.PrepareToPublish($"New version from {project.Name} project");
                var version = await client.Script.PublishAsync(model, cancellation);
                wal.Overwrite(version);

                Pushed?.Invoke(this, new PushedOneEventArgs<WalFile> { Resource = wal, Project = project, Environment = environment });
            }
        }
    }
}