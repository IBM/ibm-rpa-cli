using Microsoft.Extensions.Logging;
using System.Runtime.ExceptionServices;

namespace Joba.IBM.RPA
{
    public interface IDeployService
    {
        Task DeployAsync(IProject project, Environment environment, CancellationToken cancellation);
    }

    public class DeployService : IDeployService
    {
        private readonly ILogger logger;
        private readonly IRpaClientFactory rpaClientFactory;
        private readonly ICompiler compiler;

        public DeployService(ILogger logger, IRpaClientFactory rpaClientFactory, ICompiler compiler)
        {
            this.logger = logger;
            this.rpaClientFactory = rpaClientFactory;
            this.compiler = compiler;
        }

        async Task IDeployService.DeployAsync(IProject project, Environment environment, CancellationToken cancellation)
        {
            var buildArguments = new BuildArguments(project, new DirectoryInfo(Path.Combine(project.RpaDirectory.FullName, "build")));
            var buildResult = await compiler.BuildAsync(buildArguments, cancellation);
            if (!buildResult.Success)
                ExceptionDispatchInfo.Capture(buildResult.Error!).Throw();

            var client = rpaClientFactory.CreateFromEnvironment(environment);
            logger.LogDebug("Creating or updating project '{Project}'", project.Name);
            var serverProject = await client.Project.CreateOrUpdateAsync(project.Name, project.Description, cancellation);

            foreach (var (robot, wal) in buildResult.Robots)
            {
                logger.LogDebug("Publishing '{Script}'", wal.Name);

                PublishScript model;
                var publishComment = $"New version from {project.Name} project deployed at {DateTime.UtcNow} from {System.Environment.MachineName}";
                var latest = await client.Script.GetLatestVersionAsync(wal.Name.WithoutExtension, cancellation);
                if (latest == null)
                    model = wal.PrepareToPublish(publishComment, robot.Settings.Timeout, resetIds: true);
                else
                {
                    wal.Overwrite(latest.ScriptId, latest.Id, latest.Version);
                    model = wal.PrepareToPublish(publishComment, robot.Settings.Timeout);
                }

                var scriptVersion = await client.Script.PublishAsync(model, cancellation);
                if (robot.Settings is UnattendedSettings settings)
                {
                    if (string.IsNullOrEmpty(settings.ComputerGroupName))
                        throw new InvalidOperationException($"Cannot deploy 'unattended' bot because 'computer group' is required and was not provided");

                    logger.LogDebug("Creating or updating '{Bot}' bot ({Type})", robot.Name, "unattended");
                    var computerGroup = await client.ComputerGroup.GetAsync(settings.ComputerGroupName, cancellation);
                    var serverBot = new ServerBot(serverProject.Id, scriptVersion.ScriptId, scriptVersion.Id, computerGroup.Id, robot.Name, robot.Name.Replace(" ", "_"), string.IsNullOrEmpty(robot.Settings.Description) ? robot.Name : robot.Settings.Description);
                    await client.Bot.CreateOrUpdateAsync(serverBot, cancellation);
                }
                else
                {
                    //TODO: add support for ChatMappings (chatbot)
                    //TODO: add support for Launcher (attended)
                }
            }

            var (envFile, envSettings) = await environment.LoadSettingsAsync(project, cancellation);
            if (envFile.Exists)
                logger.LogInformation("Using '{File}' to deploy environment configurable values", envFile.FullPath);
            else
                logger.LogWarning("Environment file '{File}' not found, using project file to deploy environment configurable values", envFile.FullPath);

            var tasks = envSettings.Parameters.Select(parameter =>
            {
                logger.LogInformation("Deploying parameter {Name}", parameter.Name);
                return client.Parameter.CreateOrUpdateAsync(parameter.Name, parameter.Value, cancellation);
            }).ToArray();

            await TaskExtensions.WhenAllFailFast(tasks, cancellation);
        }
    }
}
