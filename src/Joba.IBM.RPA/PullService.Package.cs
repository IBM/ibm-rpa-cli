//namespace Joba.IBM.RPA
//{
//    public class PackagePullService
//    {
//        public PackagePullService(IRpaClient client, Project project, Environment environment)
//        {
//            One = new PullOne(client, project, environment);
//            Many = new PullMany(client, project, environment);
//        }

//        public IPullOne<WalFile> One { get; }
//        public IPullMany Many { get; }

//        class PullOne : IPullOne<WalFile>
//        {
//            private readonly Project project;
//            private readonly Environment environment;
//            private readonly IRpaClient client;

//            internal PullOne(IRpaClient client, Project project, Environment environment)
//            {
//                this.client = client;
//                this.project = project;
//                this.environment = environment;
//            }

//            public event EventHandler<ContinuePullOperationEventArgs<WalFile>>? ShouldContinueOperation;
//            public event EventHandler<PulledOneEventArgs<WalFile>>? Pulled;

//            public async Task PullAsync(string name, CancellationToken cancellation)
//            {
//                //var local = environment.Dependencies.GetParameter(name);
//                //if (local == null)
//                //{
//                //    var parameter = await GetAndThrowIfDoesNotExistAsync(name, cancellation);
//                //    project.Dependencies.AddParameter(parameter.Name);
//                //    environment.Dependencies.AddOrUpdate(parameter);
//                //    Pulled?.Invoke(this, PulledOneEventArgs<Parameter>.Created(environment, project, parameter));
//                //}
//                //else
//                //{
//                //    var args = new ContinuePullOperationEventArgs<Parameter> { Resource = local.Value, Project = project, Environment = environment };
//                //    ShouldContinueOperation?.Invoke(this, args);
//                //    if (!args.Continue.HasValue)
//                //        throw new OperationCanceledException("User did not provide an answer");
//                //    if (args.Continue.Value == false)
//                //        throw new OperationCanceledException("User cancelled the operation");

//                //    var parameter = await GetAndThrowIfDoesNotExistAsync(name, cancellation);
//                //    project.Dependencies.AddParameter(parameter.Name);
//                //    environment.Dependencies.AddOrUpdate(parameter);

//                //    PulledOneEventArgs<Parameter>? pulledArgs;
//                //    if (local.Value.Value == parameter.Value)
//                //        pulledArgs = PulledOneEventArgs<Parameter>.NoChange(environment, project, local.Value);
//                //    else
//                //        pulledArgs = PulledOneEventArgs<Parameter>.Updated(environment, project, parameter, local.Value);
//                //    Pulled?.Invoke(this, pulledArgs);
//                //}
//            }
//        }

//        class PullMany : IPullMany
//        {
//            private readonly Project project;
//            private readonly Environment environment;
//            private readonly IRpaClient client;

//            internal PullMany(IRpaClient client, Project project, Environment environment)
//            {
//                this.client = client;
//                this.project = project;
//                this.environment = environment;
//            }

//            public event EventHandler<ContinuePullOperationEventArgs>? ShouldContinueOperation;
//            public event EventHandler<PullingEventArgs>? Pulling;
//            public event EventHandler<PulledAllEventArgs>? Pulled;

//            public async Task PullAsync(CancellationToken cancellation)
//            {
//                var args = new ContinuePullOperationEventArgs { Project = project, Environment = environment };
//                ShouldContinueOperation?.Invoke(this, args);
//                if (!args.Continue.HasValue)
//                    throw new OperationCanceledException("User did not provide an answer");
//                if (args.Continue == false)
//                    throw new OperationCanceledException("User cancelled the operation");

//                Pulling?.Invoke(this, new PullingEventArgs { Project = project, Environment = environment });

//                var parameters = (await client.Parameter.SearchAsync(project.Name, 50, cancellation)).Where(s => s.Name.StartsWith(project.Name)).ToArray();
//                project.Dependencies.SetParameters(parameters.Select(p => p.Name).ToArray());
//                environment.Dependencies.AddOrUpdate(parameters);

//                Pulled?.Invoke(this, new PulledAllEventArgs { Total = parameters.Length, Project = project, Environment = environment });
//            }
//        }
//    }
//}