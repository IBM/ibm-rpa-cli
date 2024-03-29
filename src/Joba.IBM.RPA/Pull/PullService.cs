﻿namespace Joba.IBM.RPA
{
    public class PullService
    {
        private readonly IProject project;
        private readonly string alias;
        private readonly IEnumerable<IPullMany> services;

        public PullService(IProject project, string alias, params IPullMany[] services)
        {
            this.project = project;
            this.alias = alias;
            this.services = services;
        }

        public event EventHandler<ContinueOperationEventArgs>? ShouldContinueOperation;
        public event EventHandler<PullingEventArgs>? Pulling;

        public async Task PullAsync(NamePattern pattern, CancellationToken cancellation)
        {
            var args = new ContinueOperationEventArgs { Project = project, Alias = alias, Pattern = pattern };
            ShouldContinueOperation?.Invoke(this, args);
            if (!args.Continue.HasValue)
                throw new OperationCanceledException("User did not provide an answer");
            if (args.Continue == false)
                throw new OperationCanceledException("User cancelled the operation");

            foreach (var service in services)
            {
                service.ShouldContinueOperation += OnShouldContinueOperation;
                service.Pulling += OnPulling;
                await service.PullAsync(pattern, cancellation);
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
        Task PullAsync(NamePattern pattern, CancellationToken cancellation);
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
        public required IProject Project { get; init; }
        public required NamePattern Pattern { get; init; }
        public required string Alias { get; init; }
        public bool? Continue { get; set; }
    }

    public class PullingEventArgs : DownloadingEventArgs
    {
        public required string Alias { get; init; }
        public int? Total { get; init; }
        public int? Current { get; init; }
    }

    public class DownloadingEventArgs : EventArgs
    {
        public required IProject Project { get; init; }
        public string? ResourceName { get; init; }
    }

    public class PulledAllEventArgs : DownloadedEventArgs
    {
        public required NamePattern Pattern { get; init; }
        public required string Alias { get; init; }
    }

    public class DownloadedEventArgs : EventArgs
    {
        public required IProject Project { get; init; }
        public required int Total { get; init; }
    }

    public class PulledOneEventArgs<T> : EventArgs
    {
        public required IProject Project { get; init; }
        public required string Alias { get; init; }
        public required ChangeType Change { get; init; }
        public required T Resource { get; init; }
        public T? Previous { get; init; }

        public static PulledOneEventArgs<T> Created(string alias, IProject project, T resource) =>
            Factory(ChangeType.Created, alias, project, resource, default);

        public static PulledOneEventArgs<T> Updated(string alias, IProject project, T resource, T previous) =>
            Factory(ChangeType.Updated, alias, project, resource, previous);

        public static PulledOneEventArgs<T> NoChange(string alias, IProject project, T resource) =>
            Factory(ChangeType.NoChange, alias, project, resource, default);

        private static PulledOneEventArgs<T> Factory(ChangeType change, string alias, IProject project, T resource, T? previous)
        {
            return new PulledOneEventArgs<T>
            {
                Change = change,
                Alias = alias,
                Project = project,
                Resource = resource,
                Previous = previous
            };
        }

        public enum ChangeType { NoChange, Created, Updated }
    }

    public class PushedOneEventArgs<T> : EventArgs
    {
        public required IProject Project { get; init; }
        public required string EnvironmentAlias { get; init; }
        public required T Resource { get; init; }
    }
}
