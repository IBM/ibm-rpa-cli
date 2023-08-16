using Joba.IBM.RPA.Server;
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

        /// <summary>
        /// TODO: refactor this method
        /// </summary>
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
                if (robot.Settings is UnattendedSettings unattendedSettings)
                    await DeployUnattendedAsync(client, robot, scriptVersion, unattendedSettings, serverProject, cancellation);
                else if (robot.Settings is ChatbotSettings chatbotSettings)
                    await DeployChatbotAsync(client, robot, scriptVersion, chatbotSettings, environment, cancellation);
                else
                {
                    //TODO: add support for ChatMappings (chatbot)
                    //TODO: add support for Launcher (attended)
                }
            }

            //TODO: the "environment settings" should contain "RobotSettings" as well, such as 'computer-group', 'chat handle', etc.
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

        private async Task DeployChatbotAsync(IRpaClient client, Robot robot, ScriptVersion scriptVersion, ChatbotSettings settings, Environment environment, CancellationToken cancellation)
        {
            settings.EnsureValid();
            logger.LogDebug("Creating or updating '{Bot}' bot ({Type})", robot.Name, ChatbotSettings.TypeName);

            var computers = await client.Computer.SearchAsync(null, 50, cancellation);
            if (!computers.Any())
                throw new InvalidOperationException($"There are no computers registered in the server for ({environment.Alias}) {environment.Remote.TenantName}");
            var chats = await client.Chat.GetAllAsync(cancellation);
            if (!chats.Any())
                throw new InvalidOperationException($"There are no chats registered in the server for ({environment.Alias}) {environment.Remote.TenantName}.");

            logger.LogDebug("Creating or updating chat mapping '{Handle}' named '{Name}'", settings.Handle, settings.Name);
            var chat = chats.FirstOrDefault(c => c.Handle == settings.Handle!) ?? throw new InvalidOperationException($"Could not find chat '{settings.Handle!}'. Available: {string.Join(',', chats.Select(c => c.Handle))}");
            var chatComputers = computers.Where(c => settings.Computers.Contains(c.Name)).ToArray();
            var except = settings.Computers.Except(chatComputers.Select(c => c.Name)).ToArray();
            if (except.Any())
                throw new InvalidOperationException($"The following computers were not found {string.Join(',', except)}");

            var computerIds = chatComputers.Select(c => c.Id).ToArray();
            var chatMapping = new CreateChatMappingRequest(chat.Id, scriptVersion.ScriptId, scriptVersion.Id, settings.Name!, settings.Greeting, settings.Style, computerIds, settings.UnlockMachine.GetValueOrDefault());
            await client.ChatMapping.CreateOrUpdateAsync(chatMapping, cancellation);
        }

        private async Task DeployUnattendedAsync(IRpaClient client, Robot robot, ScriptVersion scriptVersion, UnattendedSettings settings, Server.Project serverProject, CancellationToken cancellation)
        {
            settings.EnsureValid();
            logger.LogDebug("Creating or updating '{Bot}' bot ({Type})", robot.Name, UnattendedSettings.TypeName);
            var computerGroup = await client.ComputerGroup.GetAsync(settings.ComputerGroupName!, cancellation);
            var botRequest = new CreateBotRequest(serverProject.Id, scriptVersion.ScriptId, scriptVersion.Id, computerGroup.Id, robot.Name, new UniqueId(robot.Name), string.IsNullOrEmpty(robot.Settings.Description) ? robot.Name : robot.Settings.Description);
            await client.Bot.CreateOrUpdateAsync(botRequest, cancellation);
        }
    }
}
