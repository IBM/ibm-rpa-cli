namespace Joba.Pipeline
{
    /// <summary>
    /// Defines and executes a pipeline.
    /// Based on: https://github.com/ipvalverde/PipelineNet
    /// </summary>
    /// <typeparam name="TContext">The parameter type.</typeparam>
    sealed class AsyncPipeline<TContext> : IAsyncPipeline<TContext>
    {
        private readonly IList<IAsyncMiddleware<TContext>> middlewares;
        private IAsyncMiddleware<TContext> @finally = EmptyMiddleware.None;
        private Func<TContext, PipelineExceptionSource, CancellationToken, Task> @catch;

        private AsyncPipeline()
        {
            middlewares = new List<IAsyncMiddleware<TContext>>();
        }

        IAsyncPipeline<TContext> IAsyncPipeline<TContext>.Add(params IAsyncMiddleware<TContext>[] middlewares)
        {
            foreach (var middleware in middlewares)
                this.middlewares.Add(middleware);
            return this;
        }

        IAsyncPipeline<TContext> IAsyncPipeline<TContext>.Catch(Func<TContext, PipelineExceptionSource, CancellationToken, Task> func)
        {
            @catch = func;
            return this;
        }

        IAsyncPipeline<TContext> IAsyncPipeline<TContext>.Finally(IAsyncMiddleware<TContext> middleware)
        {
            @finally = middleware;
            return this;
        }
        IAsyncPipeline<TContext> IAsyncPipeline<TContext>.Finally(Func<TContext, CancellationToken, Task> func)
        {
            @finally = new EmptyMiddleware(func);
            return this;
        }

        Task IAsyncPipeline<TContext>.ExecuteAsync(TContext context, CancellationToken cancellation) => ExecuteAsync(context, cancellation);
        Task IAsyncPipeline<TContext>.ExecuteAsync(TContext context) => ExecuteAsync(context, CancellationToken.None);

        private async Task ExecuteAsync(TContext context, CancellationToken cancellation)
        {
            if (!middlewares.Any())
                return;

            var index = 0;
            Func<TContext, Task> action = null;
            action = async (param) =>
            {
                cancellation.ThrowIfCancellationRequested();

                var middleware = middlewares[index];
                index++;

                // If the current instance of middleware is the last one in the list, the "next" function is return true.
                if (index == middlewares.Count)
                    action = (p) => Task.CompletedTask;

                await middleware.Run(param, action, cancellation).ConfigureAwait(false);
            };

            try
            {
                await action(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var exceptionSource = new PipelineExceptionSource(ex);
                if (@catch != null)
                    await @catch.Invoke(context, exceptionSource, cancellation).ConfigureAwait(false);

                if (!exceptionSource.Handled)
                    throw;
            }
            finally
            {
                await @finally.Run(context, p => Task.CompletedTask, cancellation).ConfigureAwait(false);
            }
        }

        public static IAsyncPipeline<TContext> Create() => new AsyncPipeline<TContext>();

        class EmptyMiddleware : IAsyncMiddleware<TContext>
        {
            public static EmptyMiddleware None = new EmptyMiddleware();
            private readonly Func<TContext, CancellationToken, Task> @finally;

            private EmptyMiddleware() { }

            public EmptyMiddleware(Func<TContext, CancellationToken, Task> @finally)
            {
                this.@finally = @finally;
            }

            Task IAsyncMiddleware<TContext>.Run(TContext context, Func<TContext, Task> next, CancellationToken cancellation)
            {
                if (@finally != null)
                    return @finally(context, cancellation);

                return Task.FromResult(true);
            }
        }
    }
}
