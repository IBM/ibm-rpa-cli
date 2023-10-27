using Joba.IBM.RPA.Server;

namespace Joba.IBM.RPA
{
    public class ParameterPullService
    {
        public ParameterPullService(IRpaClient client, IProject project, string alias)
        {
            One = new PullOne(client, project, alias);
            Many = new PullMany(client, project, alias);
        }

        public IPullOne<Parameter> One { get; }
        public IPullMany Many { get; }

        class PullOne : IPullOne<Parameter>
        {
            private readonly IProject project;
            private readonly string alias;
            private readonly IRpaClient client;

            internal PullOne(IRpaClient client, IProject project, string alias)
            {
                this.client = client;
                this.project = project;
                this.alias = alias;
            }

            public event EventHandler<ContinueOperationEventArgs<Parameter>>? ShouldContinueOperation;
            public event EventHandler<PulledOneEventArgs<Parameter>>? Pulled;

            public async Task PullAsync(string name, CancellationToken cancellation)
            {
                var local = project.Parameters.Get(name);
                if (local == null)
                {
                    var parameter = await GetAndThrowIfDoesNotExistAsync(name, cancellation);
                    project.Parameters.AddOrUpdate(parameter);

                    Pulled?.Invoke(this, PulledOneEventArgs<Parameter>.Created(alias, project, parameter));
                }
                else
                {
                    var args = new ContinueOperationEventArgs<Parameter> { Resource = local, Project = project, Alias = alias, Pattern = new NamePattern(name) };
                    ShouldContinueOperation?.Invoke(this, args);
                    if (!args.Continue.HasValue)
                        throw new OperationCanceledException("User did not provide an answer");
                    if (args.Continue.Value == false)
                        throw new OperationCanceledException("User cancelled the operation");

                    var parameter = await GetAndThrowIfDoesNotExistAsync(name, cancellation);
                    project.Parameters.AddOrUpdate(parameter);

                    PulledOneEventArgs<Parameter>? pulledArgs;
                    if (local.Value == parameter.Value)
                        pulledArgs = PulledOneEventArgs<Parameter>.NoChange(alias, project, local);
                    else
                        pulledArgs = PulledOneEventArgs<Parameter>.Updated(alias, project, parameter, local);
                    Pulled?.Invoke(this, pulledArgs);
                }
            }

            private async Task<Parameter> GetAndThrowIfDoesNotExistAsync(string name, CancellationToken cancellation)
            {
                var parameter = await client.Parameter.GetAsync(name, cancellation);
                if (parameter == null)
                    throw new Exception($"Could not find the parameter '{name}'");

                return parameter;
            }
        }

        class PullMany : IPullMany
        {
            private readonly IProject project;
            private readonly string alias;
            private readonly IRpaClient client;

            internal PullMany(IRpaClient client, IProject project, string alias)
            {
                this.client = client;
                this.project = project;
                this.alias = alias;
            }

            public event EventHandler<ContinueOperationEventArgs>? ShouldContinueOperation;
            public event EventHandler<PullingEventArgs>? Pulling;
            public event EventHandler<PulledAllEventArgs>? Pulled;

            public async Task PullAsync(NamePattern pattern, CancellationToken cancellation)
            {
                var args = new ContinueOperationEventArgs { Project = project, Alias = alias, Pattern = pattern };
                ShouldContinueOperation?.Invoke(this, args);
                if (!args.Continue.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (args.Continue == false)
                    throw new OperationCanceledException("User cancelled the operation");

                Pulling?.Invoke(this, new PullingEventArgs { Project = project, Alias = alias });

                var task = pattern.HasWildcard ? PullWildcardAsync(pattern, cancellation) : PullFixedAsync(pattern.Name, cancellation);
                var parameters = (await task).ToArray();

                project.Parameters.AddOrUpdate(parameters);

                Pulled?.Invoke(this, new PulledAllEventArgs { Total = parameters.Length, Project = project, Alias = alias, Pattern = pattern });
            }

            private async Task<IEnumerable<Parameter>> PullWildcardAsync(NamePattern pattern, CancellationToken cancellation) =>
                await client.Parameter.SearchAsync(pattern.Name, 50, cancellation)
                    .ContinueWith(c => c.Result.Where(s => pattern.Matches(s.Name)), TaskContinuationOptions.OnlyOnRanToCompletion);

            private async Task<IEnumerable<Parameter>> PullFixedAsync(string parameterName, CancellationToken cancellation)
            {
                var parameter = await client.Parameter.GetAsync(parameterName, cancellation);
                if (parameter == null)
                    throw new Exception($"Parameter '{parameterName}' does not exist.");
                return new Parameter[] { parameter };
            }
        }
    }
}