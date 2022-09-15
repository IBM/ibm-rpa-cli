namespace Joba.IBM.RPA
{
    public class PullService
    {
        private readonly Project project;
        private readonly Environment environment;
        private readonly IEnumerable<IPullMany> services;

        public PullService(Project project, Environment environment, params IPullMany[] services)
        {
            this.project = project;
            this.environment = environment;
            this.services = services;
        }

        public event EventHandler<ContinueOperationEventArgs>? ShouldContinueOperation;
        public event EventHandler<PullingEventArgs>? Pulling;

        public async Task PullAsync(CancellationToken cancellation)
        {
            var args = new ContinueOperationEventArgs { Project = project, Environment = environment };
            ShouldContinueOperation?.Invoke(this, args);
            if (!args.Continue.HasValue)
                throw new OperationCanceledException("User did not provide an answer");
            if (args.Continue == false)
                throw new OperationCanceledException("User cancelled the operation");

            foreach (var service in services)
            {
                service.ShouldContinueOperation += OnShouldContinueOperation;
                service.Pulling += OnPulling;
                await service.PullAsync(cancellation);
            }
        }

        private void OnShouldContinueOperation(object? sender, ContinueOperationEventArgs e) => e.Continue = true;

        private void OnPulling(object? sender, PullingEventArgs e)
        {
            Pulling?.Invoke(this, e);
        }
    }

    public interface IPullOne<T>
    {
        event EventHandler<ContinueOperationEventArgs<T>>? ShouldContinueOperation;
        event EventHandler<PulledOneEventArgs<T>>? Pulled;
        Task PullAsync(string name, CancellationToken cancellation);
    }

    public interface IPullMany
    {
        event EventHandler<ContinueOperationEventArgs>? ShouldContinueOperation;
        event EventHandler<PullingEventArgs>? Pulling;
        event EventHandler<PulledAllEventArgs>? Pulled;
        Task PullAsync(CancellationToken cancellation);
    }

    public interface IPushOne<T>
    {
        event EventHandler<ContinueOperationEventArgs<T>>? ShouldContinueOperation;
        event EventHandler<PushedOneEventArgs<T>>? Pushed;
        Task PushAsync(string name, CancellationToken cancellation);
    }

    public class ContinueOperationEventArgs<T> : ContinueOperationEventArgs
    {
        public required T Resource { get; init; }
    }

    public class ContinueOperationEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public bool? Continue { get; set; }
    }

    public class PullingEventArgs : DownloadingEventArgs
    {
        public required Environment Environment { get; init; }
        public int? Total { get; init; }
        public int? Current { get; init; }
    }

    public class DownloadingEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public string? ResourceName { get; init; }
    }

    public class PulledAllEventArgs : DownloadedEventArgs
    {
        public required Environment Environment { get; init; }
    }

    public class DownloadedEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public required int Total { get; init; }
    }

    public class PulledOneEventArgs<T> : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public required ChangeType Change { get; init; }
        public required T Resource { get; init; }
        public T? Previous { get; init; }

        public static PulledOneEventArgs<T> Created(Environment environment, Project project, T resource) =>
            Factory(ChangeType.Created, environment, project, resource, default);

        public static PulledOneEventArgs<T> Updated(Environment environment, Project project, T resource, T previous) =>
            Factory(ChangeType.Updated, environment, project, resource, previous);

        public static PulledOneEventArgs<T> NoChange(Environment environment, Project project, T resource) =>
            Factory(ChangeType.NoChange, environment, project, resource, default);

        private static PulledOneEventArgs<T> Factory(ChangeType change, Environment environment, Project project, T resource, T? previous)
        {
            return new PulledOneEventArgs<T>
            {
                Change = change,
                Environment = environment,
                Project = project,
                Resource = resource,
                Previous = previous
            };
        }

        public enum ChangeType { NoChange, Created, Updated }
    }

    public class PushedOneEventArgs<T> : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public required T Resource { get; init; }
    }
}
