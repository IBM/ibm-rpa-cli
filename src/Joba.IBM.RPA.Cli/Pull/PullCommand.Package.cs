namespace Joba.IBM.RPA.Cli
{
	partial class PullCommand
	{
		[RequiresEnvironment]
		class PullPackageCommand : Command
		{
			private readonly ShallowEnvironmentRenderer environmentRenderer = new();

			public PullPackageCommand() : base("package", "Pulls package dependencies")
			{
				var name = new Argument<string?>("name", "The specific package name") { Arity = ArgumentArity.ZeroOrOne };
				AddArgument(name);

				this.SetHandler(HandleAsync, name,
					Bind.FromServiceProvider<Project>(),
					Bind.FromServiceProvider<Environment>(),
					Bind.FromServiceProvider<InvocationContext>());
			}

			private async Task HandleAsync(string? name, Project project, Environment environment, InvocationContext context)
			{
				var cancellation = context.GetCancellationToken();
				var client = RpaClientFactory.CreateFromEnvironment(environment);
				var pullService = new WalPullService(client, project, environment);

				if (!string.IsNullOrEmpty(name))
				{
					pullService.One.ShouldContinueOperation += OnShouldPullingOneFile;
					pullService.One.Pulled += OnPulled;
					await pullService.One.PullAsync(name, cancellation);
				}
				else
				{
					pullService.Many.ShouldContinueOperation += OnShouldPullingAllFiles;
					pullService.Many.Pulling += OnPulling;
					pullService.Many.Pulled += OnPulled;
					await pullService.Many.PullAsync(cancellation);
					StatusCommand.Handle(project, environment);
				}

				await project.SaveAsync(cancellation);
				await environment.SaveAsync(cancellation);
			}

			private void OnPulled(object? sender, PulledOneEventArgs<WalFile> e)
			{
				ExtendedConsole.Write($"From ");
				environmentRenderer.RenderLine(e.Environment);
				if (e.Change == PulledOneEventArgs<WalFile>.ChangeType.NoChange)
					ExtendedConsole.WriteLineIndented($"No change. {e.Resource.Info.Name:blue} is already in the latest {e.Resource.Version:blue} version.");
				else if (e.Change == PulledOneEventArgs<WalFile>.ChangeType.Created)
					ExtendedConsole.WriteLineIndented($"{e.Resource.Info.Name:blue} has been created from the latest server {e.Resource.Version:green} version.");
				else
				{
					var previousVersion = e.Previous!.Version.HasValue ? e.Previous.Version.ToString() : "local";
					ExtendedConsole.WriteLineIndented($"{e.Resource.Info.Name:blue} has been updated from {previousVersion:darkgray} to {e.Resource.Version:green} version. " +
						$"Close the file in Studio and open it again.");
				}
			}

			private void OnPulled(object? sender, PulledAllEventArgs e)
			{
				if (e.Total == 0)
					ExtendedConsole.WriteLine($"No files found for {e.Project.Name:blue} project.");
			}

			private void OnPulling(object? sender, PullingEventArgs e)
			{
				Console.Clear();
				ExtendedConsole.WriteLine($"Pulling files from {e.Project.Name:blue} project...");
				if (e.Current.HasValue && e.Total.HasValue && !string.IsNullOrEmpty(e.ResourceName))
					ExtendedConsole.WriteLineIndented($"({e.Current}/{e.Total}) pulling {e.ResourceName:blue}");
			}

			private void OnShouldPullingAllFiles(object? sender, ContinueOperationEventArgs e)
			{
				e.Continue = ExtendedConsole.YesOrNo(
					$"This operation will pull the latest server file versions of {e.Project.Name:blue} project. " +
					$"If there are local copies in the {e.Environment.Alias:blue} ({e.Environment.Directory}) directory, they will be overwritten. This is irreversible. " +
					$"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
			}

			private void OnShouldPullingOneFile(object? sender, ContinueOperationEventArgs<WalFile> e)
			{
				e.Continue = ExtendedConsole.YesOrNo($"This operation will fetch and overwrite the file {e.Resource.Info.Name:blue} with the latest server version. This is irreversible. " +
					$"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
			}
		}
	}
}
