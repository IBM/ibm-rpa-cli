using System.Diagnostics;

namespace Joba.IBM.RPA
{
    internal static class TaskExtensions
    {
        /// <summary>
        /// Fails the <see cref="Task.WhenAll(Task[])"/> when one of the tasks fails.
        /// From https://stackoverflow.com/a/69338551/1830639
        /// </summary>
        internal static Task<TResult[]> WhenAllFailFast<TResult>(Task<TResult>[] tasks, CancellationToken cancellation)
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
    }
}
