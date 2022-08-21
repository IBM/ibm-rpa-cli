namespace Joba.IBM.RPA
{
    public interface IPullOne<T>
    {
        event EventHandler<ContinuePullOperationEventArgs<T>>? ShouldContinueOperation;
        event EventHandler<PulledOneEventArgs<T>>? Pulled;
        Task PullAsync(string name, CancellationToken cancellation);
    }

    public interface IPullMany<T>
    {
        event EventHandler<ContinuePullOperationEventArgs>? ShouldContinueOperation;
        event EventHandler<PullingEventArgs>? Pulling;
        event EventHandler<PulledAllEventArgs<T>>? Pulled;
        Task PullAsync(CancellationToken cancellation);
    }

    public class ContinuePullOperationEventArgs<T> : ContinuePullOperationEventArgs
    {
        public required T Resource { get; init; }
    }

    public class ContinuePullOperationEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public bool? Continue { get; set; }
    }

    public class PullingEventArgs : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public string? ResourceName { get; init; }
        public int? Total { get; init; }
        public int? Current { get; init; }
    }

    public class PulledAllEventArgs<T> : EventArgs
    {
        public required Project Project { get; init; }
        public required Environment Environment { get; init; }
        public required IEnumerable<T> Resources { get; init; }
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
}
