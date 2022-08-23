using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Xml.Linq;

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
             
             * - rpa push (pushes all the project files - wal & parameters)
             * - rpa push <name> (pushes the wal file)
             * - rpa push --parameter (pushes only parameters)
             * - rpa push <name> --parameter (pushes the parameter)
             * 
             * - rpa fetch (fetches the files and compare with the local copy, showing either they match or not)
             * - rpa fetch <name> (fetches the wal file)
             * - rpa fetch --parameter (fetches only parameters)
             * - rpa fetch <name> --parameter (fetches the parameter)
             * 
             * - rpa tag <tagname> 
             *    (tags the current local files to a version. This would be used in the 'promote', to assert that
             *     those files versions will be the exact ones that gets promoted. This tag would be saved within the project configuration
             *     with all the files, their versions, and hashes, so when 'promote' is called, we can compare the server hash with the tag hash)
             * - rpa promote <tag> <env> 
             *    (promote the project tag to another environment, by downloading the tagged files versions on a 'staging area',
             *     and pushing them to the new <env> if the hash matches, then delete the staging.)             
             * Roadmap
             * - read wal files to get the 'parameters' they are using and create a local file with it
             *   - allow specifying different values in other environments
             * - handle chatbot settings
             * - handle credentials
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