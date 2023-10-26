namespace Joba.Pipeline
{
    /// <summary>
    /// Default implementation of <see cref="IAsyncMiddleware{TParameter}"/>.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    abstract class DefaultPipelineMiddleware<TContext> : IAsyncMiddleware<TContext>
    {
        /// <summary>
        /// Override this method to implement the middleware logic.
        /// This method automatically calls the next execution in the pipeline at the end.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        protected abstract Task Run(TContext context, CancellationToken cancellation);

        async Task IAsyncMiddleware<TContext>.Run(TContext context, Func<TContext, Task> next, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            await Run(context, cancellation).ConfigureAwait(false);
            await next(context);
        }
    }
}
