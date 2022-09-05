using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Reflection;

namespace Joba.IBM.RPA.Cli
{
    static class Program
    {
        public static Task<int> Main(string[] args)
        {
            //https://docs.microsoft.com/en-us/dotnet/standard/commandline/
            //example: https://github.com/dotnet/command-line-api/issues/1776#issuecomment-1165482490
            //bind class: https://github.com/dotnet/command-line-api/issues/1750#issuecomment-1152707726

            /*
             * Main purpose: manage "projects" and "environments" (tenants)
             * Commands ideas
             * - rpa pull (pulls all the project files - wal & parameters)
             * - rpa pull <name> (pulls the wal file)
             * - rpa pull --parameter (pulls only parameters)
             * - rpa pull <name> --parameter (pulls the parameter)
             * 
             * - rpa package source <alias> (add a package source - a tenant where only packages scripts lives - nothing else)
             *   (to create packages, you can start a "package project" like you would, and "deploy" them to the "package" tenant)
             *   (to consume packages in other projects, you would use "rpa package install <name>" and that would query the "source alias")
             *   
             * - rpa package install <name> (intalls packages from source)
             * - rpa package restore (restores the packages from source - downloads the files locally into "packages" folder)
             *   (developers should use 'executeScript' with local files - with 'rpa deploy' we will update the 'executeScript' commands)
             *   
             * - rpa deploy <env> (deploys all the scripts, including packages, to the environment)
             *   (we do not need 'rpa push', because this should be handled by GIT)
             * 
             * - rpa promote <tag> <env> 
             *    (promote the project tag to another environment, by downloading the tagged files versions on a 'staging area',
             *     and pushing them to the new <env> if the hash matches, then delete the staging.)             
             */

            Directory.CreateDirectory(Constants.LocalFolder);

            var parser = new CommandLineBuilder(new RpaCommand())
                .UseHelp()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler(OnException)
                .CancelOnProcessTermination()
                .AddMiddleware(Middleware)
                .Build();

            return parser.InvokeAsync(args);
        }

        private static void OnException(Exception exception, InvocationContext context)
        {
            exception.Trace();

            using (ExtendedConsole.BeginForegroundColor(ConsoleColor.Red))
            {
                Console.WriteLine(exception.Message);
                PackageAlreadyInstalledException(exception);
                PackageNotFoundException(exception);
            }

            void PackageAlreadyInstalledException(Exception exception)
            {
                if (exception is PackageAlreadyInstalledException ex)
                    Console.WriteLine($"Use '{RpaCommand.CommandName} {PackageCommand.UpdatePackageCommand.CommandName} {ex.PackageName}' to update it.");
            }

            void PackageNotFoundException(Exception exception)
            {
                if (exception is PackageNotFoundException ex)
                    Console.WriteLine($"Use '{RpaCommand.CommandName} {PackageCommand.InstallPackageCommand.CommandName} {ex.PackageName}' to install it first.");
            }
        }

        private static async Task Middleware(InvocationContext context, Func<InvocationContext, Task> next)
        {
            if (context.ParseResult.CommandResult != context.ParseResult.RootCommandResult &&
                context.ParseResult.CommandResult.Command.GetType() != typeof(ProjectCommand))
                await LoadProjectAsync(context);

            await next(context);
        }

        private static async Task LoadProjectAsync(InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var project = await ProjectFactory.LoadFromCurrentDirectoryAsync(cancellation);
            var environment = await project.GetCurrentEnvironmentAsync(cancellation);

            if (context.ParseResult.CommandResult.Command.GetType().GetCustomAttribute<RequiresEnvironmentAttribute>() != null
                && environment == null)
                throw EnvironmentException.NoEnvironment(string.Join(" ", context.ParseResult.Tokens.Select(f => f.Value)));

            context.BindingContext.AddService(s => project);
            if (environment != null)
                context.BindingContext.AddService(s => environment);
        }
    }
}