namespace Joba.Pipeline
{
    /// <summary>
    /// Defines and executes a pipeline.
    /// </summary>
    /// <typeparam name="TContext">The parameter type.</typeparam>
    public interface IAsyncPipeline<TContext>
    {
        /// <summary>
        /// Adds a middleware.
        /// </summary>
        /// <param name="middlewares">The middleware.</param>
        IAsyncPipeline<TContext> Add(params IAsyncMiddleware<TContext>[] middlewares);

        /// <summary>
        /// Configures a "catch" function that is executed if an exception is thrown by other middlewares.
        /// </summary>
        /// <param name="func">The function.</param>
        IAsyncPipeline<TContext> Catch(Func<TContext, PipelineExceptionSource, CancellationToken, Task> func);

        /// <summary>
        /// Configures a "finally" middleware that is executed even an exception is thrown by other middlewares.
        /// </summary>
        /// <param name="middleware">The middleware.</param>
        IAsyncPipeline<TContext> Finally(IAsyncMiddleware<TContext> middleware);
        /// <summary>
        /// Configures a "finally" function that is executed even an exception is thrown by other middlewares.
        /// </summary>
        /// <param name="func">The function.</param>
        IAsyncPipeline<TContext> Finally(Func<TContext, CancellationToken, Task> func);

        /// <summary>
        /// Executes the middlewares in the order of their definition.
        /// </summary>
        /// <param name="context">The parameter.</param>
        /// <param name="cancellation">The cancellation token.</param>
        Task ExecuteAsync(TContext context, CancellationToken cancellation);
        /// <summary>
        /// Executes the middlewares in the order of their definition.
        /// </summary>
        /// <param name="context">The parameter.</param>
        Task ExecuteAsync(TContext context);
    }
}
