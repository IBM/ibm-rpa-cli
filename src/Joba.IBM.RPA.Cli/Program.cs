using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;

namespace Joba.IBM.RPA.Cli
{
    static class Program
    {
        public static Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder(new RpaCommand())
                .AddInstrumentation()
                .ConfigureLoggers()
                .TrackUsage()
                .UseHelp()
                .UseVersionOption()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .CancelOnProcessTermination()
                .UseExceptionHandler(OnException)
                .AddMiddleware(Middleware)
                .Build();

            return parser.InvokeAsync(args);
        }

        private static void OnException(Exception exception, InvocationContext context)
        {
            var loggerFactory = context.BindingContext.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<RpaCommand>();
            logger.LogError(exception, exception.Message);

            EnvironmentException(exception);
            PackageAlreadyInstalledException(exception);
            PackageNotFoundException(exception);
            PackageException(exception);

            PackageSourceNotFoundException(exception);
            PackageSourceException(exception);
            context.ExitCode = -1;

            void EnvironmentException(Exception exception)
            {
                if (exception is EnvironmentException ex)
                    logger.LogInformation(" Use '{RpaCommandName} {EnvironmentCommand} {AddEnvironmentCommand} {Alias}' to configure it.", RpaCommand.CommandName, EnvironmentCommand.CommandName, EnvironmentCommand.NewEnvironmentCommand.CommandName, ex.Alias);
            }

            void PackageAlreadyInstalledException(Exception exception)
            {
                if (exception is PackageAlreadyInstalledException ex)
                    logger.LogInformation(" Use '{RpaCommandName} {PackageCommandName} {UpdatePackageCommandName} {PackageName}' to update it.", RpaCommand.CommandName, PackageCommand.CommandName, PackageCommand.UpdatePackageCommand.CommandName, ex.PackageName);
            }

            void PackageNotFoundException(Exception exception)
            {
                if (exception is PackageNotFoundException ex)
                    logger.LogInformation(" Use '{RpaCommandName} {PackageCommandName} {InstallPackageCommandName} {PackageName}' to install it first.", RpaCommand.CommandName, PackageCommand.CommandName, PackageCommand.InstallPackageCommand.CommandName, ex.PackageName);
            }

            void PackageException(Exception exception)
            {
                if (exception is PackageException ex)
                    logger.LogInformation(" Use '{RpaCommandName} {PackageCommandName}' to manage packages.", RpaCommand.CommandName, PackageCommand.CommandName);
            }

            void PackageSourceNotFoundException(Exception exception)
            {
                if (exception is PackageSourceNotFoundException ex)
                    logger.LogInformation(" Use '{RpaCommandName} {PackageCommandName} {PackageSourceCommandName} {Alias}' to add it first.", RpaCommand.CommandName, PackageCommand.CommandName, PackageCommand.PackageSourceCommand.CommandName, ex.Alias);
            }

            void PackageSourceException(Exception exception)
            {
                if (exception is PackageSourceException ex)
                    logger.LogInformation(" Use '{RpaCommandName} {PackageCommandName} {PackageSourceCommandName}' to add package sources.", RpaCommand.CommandName, PackageCommand.CommandName, PackageCommand.PackageSourceCommand.CommandName);
            }
        }

        private static async Task Middleware(InvocationContext context, Func<InvocationContext, Task> next)
        {
            var loggerFactory = context.BindingContext.GetRequiredService<ILoggerFactory>();
            context.BindingContext.AddService<ISecretProvider>(s => new DefaultSecretProvider());
            context.BindingContext.AddService<IRpaHttpClientFactory>(s => new RpaHttpClientFactory(loggerFactory.CreateLogger<RpaClientFactory>()));
            context.BindingContext.AddService<IAccountAuthenticatorFactory>(s => new AccountAuthenticatorFactory(
                s.GetRequiredService<IRpaClientFactory>(), s.GetRequiredService<IRpaHttpClientFactory>()));
            context.BindingContext.AddService<IRpaClientFactory>(s => new RpaClientFactory(
                context.Console, s.GetRequiredService<IRpaHttpClientFactory>()));
            context.BindingContext.AddService<IDeployService>(s => new DeployService(
                loggerFactory.CreateLogger<DeployService>(),
                s.GetRequiredService<IRpaClientFactory>(),
                s.GetRequiredService<ICompiler>()));
            context.BindingContext.AddService<IPackageManagerFactory>(s => new PackageManagerFactory(s.GetRequiredService<IRpaClientFactory>()));
            context.BindingContext.AddService<ICompiler>(s => new Bluewasher(
                loggerFactory.CreateLogger<Bluewasher>(),
                s.GetRequiredService<IPackageManagerFactory>()));

            if (context.ParseResult.CommandResult != context.ParseResult.RootCommandResult &&
                context.ParseResult.CommandResult.Command != null)
                await TryLoadProjectAsync(context);

            await next(context);
        }

        private static async Task TryLoadProjectAsync(InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var commandType = context.ParseResult.CommandResult.Command.GetType();
            var project = await ProjectFactory.TryLoadFromCurrentDirectoryAsync(cancellation);
            if (commandType.GetCustomAttribute<RequiresProjectAttribute>() != null && project == null)
                throw ProjectException.ThrowRequired(System.Environment.CommandLine);

            if (project != null)
                context.BindingContext.AddService(s => project);
        }
    }
}