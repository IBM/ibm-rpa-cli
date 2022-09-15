namespace Joba.Pipeline
{
    /// <summary>
    /// Defines a piece of code (a step) to be executed inside a pipeline.
    /// </summary>
    /// <typeparam name="TParameter">The parameter type.</typeparam>
    public interface IAsyncMiddleware<TParameter>
    {
        /// <summary>
        /// Runs this middleware and enables calling the next middleware.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <param name="next">The next middleware to be executed. Note that implementation must call this function in order to execute the next middleware.</param>
        /// <param name="cancellation">The cancellation token.</param>
        Task Run(TParameter parameter, Func<TParameter, Task> next, CancellationToken cancellation);
    }
}
