namespace Joba.IBM.RPA.Cli
{
    class PullService
    {
        private readonly Project project;
        private readonly Environment environment;
        private readonly IRpaClient client;

        public PullService(Project project, Environment environment)
        {
            this.project = project;
            this.environment = environment;
            client = RpaClientFactory.CreateClient(environment);
        }

        public async Task AllAsync(CancellationToken cancellation)
        {
            var choice = ExtendedConsole.YesOrNo($"This operation will fetch the latest server versions of wal files which names start with {project.Name:blue}. " +
                    $"If there are local copies in the {environment.Alias:blue} ({environment.Directory}) directory, they will be overwritten. This is irreversible. " +
                    $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);

            if (!choice.HasValue)
                throw new OperationCanceledException("User did not provide an answer");
            if (choice.Value == false)
                throw new OperationCanceledException("User cancelled the operation");

            ExtendedConsole.WriteLine($"Fetching files from {project.Name:blue} project...");
            var scripts = (await client.Script.SearchAsync(project.Name, 50, cancellation)).Where(s => s.Name.StartsWith(project.Name)).ToArray();

            for (var index = 0; index < scripts.Length; index++)
            {
                var script = scripts[index];
                Console.Clear();
                ExtendedConsole.WriteLine($"Fetching files from {project.Name:blue} project...");
                ExtendedConsole.WriteLineIndented($"({index + 1}/{scripts.Length}) fetching {script.Name:blue}");

                var wal = environment.GetLocalWal(script.Name);
                if (wal == null)
                    _ = await environment.CreateWalAsync(client.Script, script.Name, cancellation);
                else
                    await wal.OverwriteToLatestAsync(client.Script, script.Name, cancellation);
            }
        }

        public async Task OneAsync(string fileName, CancellationToken cancellation)
        {
            var envRenderer = new EnvironmentRenderer();
            var wal = environment.GetLocalWal(fileName);
            if (wal == null)
            {
                wal = await environment.CreateWalAsync(client.Script, fileName, cancellation);
                ExtendedConsole.Write($"From ");
                envRenderer.RenderLine(environment);
                ExtendedConsole.WriteLineIndented($"{wal.Info.Name:blue} has been created from the latest server version {wal.Version:green}");
            }
            else if (!wal.IsFromServer)
            {
                var choice = ExtendedConsole.YesOrNo($"The wal file {wal.Info.Name:blue} is not downloaded from the server. " +
                    $"This operation will fetch and overwrite the file content with the latest server version. This is irreversible. " +
                    $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);

                if (!choice.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (choice.Value == false)
                    throw new OperationCanceledException("User cancelled the operation");

                var previousVersion = "local";
                await wal.OverwriteToLatestAsync(client.Script, fileName, cancellation);

                ExtendedConsole.Write($"From ");
                envRenderer.RenderLine(environment);
                ExtendedConsole.WriteLineIndented($"{wal.Info.Name:blue} has been updated from {previousVersion:darkgray} to {wal.Version:green} version. " +
                        $"Close the file in Studio and open it again.");
            }
            else
            {
                var choice = ExtendedConsole.YesOrNo($"This operation will fetch and overwrite the file {wal.Info.Name:blue} with the latest server version. This is irreversible. " +
                    $"Are you sure you want to continue? [y/n]", ConsoleColor.Yellow);
                if (!choice.HasValue)
                    throw new OperationCanceledException("User did not provide an answer");
                if (choice.Value == false)
                    throw new OperationCanceledException("User cancelled the operation");

                var previousVersion = wal.Version;
                await wal.UpdateToLatestAsync(client.Script, cancellation);

                ExtendedConsole.Write($"From ");
                envRenderer.RenderLine(environment);
                if (previousVersion == wal.Version)
                    ExtendedConsole.WriteLineIndented($"No change. {wal.Info.Name:blue} is already in the latest {wal.Version:blue} version");
                else
                    ExtendedConsole.WriteLineIndented($"{wal.Info.Name:blue} has been updated from {previousVersion:darkgray} to {wal.Version:green} version. " +
                        $"Close the file in Studio and open it again.");
            }
        }
    }
}