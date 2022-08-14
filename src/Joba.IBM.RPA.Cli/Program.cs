using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

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
             * Premises: update 'executeScript' to use '--version' parameter to gain performance since it caches scripts that way.
             * - connect to more than one environment within a folder
             * - create 'dev' 'test' and 'prod' subfolders
             * - fetch the latest 'environment' wal files
             * Roadmap
             * - read wal files to get the 'parameters' they are using and create a local file with it
             *   - allow specifying different values in other environments
             * - handle chatbot settings
             * - handle credentials
             */

            Directory.CreateDirectory(Constants.LocalFolder);

            var command = new RootCommand("Provides features to manage RPA through the command line");
            command.AddCommand(new ProjectCommand());
            command.AddCommand(new EnvironmentCommand());
            command.AddCommand(new StatusCommand());
            command.AddCommand(new FetchCommand());
            command.AddCommand(new SwitchCommand());
            command.SetHandler(ShowHelp);

            var parser = new CommandLineBuilder(command)
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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(exception.Message);
            Console.ResetColor();
        }

        private static void ShowHelp(InvocationContext context)
        {
            using var output = context.Console.Out.CreateTextWriter();
            var helpContext = new HelpContext(context.HelpBuilder,
                                              context.ParseResult.CommandResult.Command,
                                              output,
                                              context.ParseResult);

            context.HelpBuilder.Write(helpContext);
        }

        private static async Task Middleware(InvocationContext context, Func<InvocationContext, Task> next)
        {
            if (context.ParseResult.CommandResult != context.ParseResult.RootCommandResult
                && context.ParseResult.CommandResult.Command.GetType() != typeof(ProjectCommand))
            {
                await LoadProjectAsync(context);
            }

            await next(context);
        }

        private static async Task LoadProjectAsync(InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var project = await Project.LoadFromCurrentDirectoryAsync(cancellation);
            //var client = project.CreateClient();

            context.BindingContext.AddService(s => project);
            //context.BindingContext.AddService(s => client);
        }
    }
}