using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;

namespace Joba.IBM.RPA.Cli
{
    static class CommandLineBuilderExtensions
    {
        internal static CommandLineBuilder AddInstrumentation(this CommandLineBuilder builder)
        {
            CreateTracer(builder);
            RegisterActivitySource(builder);
            return builder;

            static void RegisterActivitySource(CommandLineBuilder builder)
            {
                builder.AddMiddleware(async (context, next) =>
                {
                    var source = new ActivitySource(RpaCommand.AssemblyName, RpaCommand.AssemblyVersion);
                    context.BindingContext.AddService(s => source);

                    await next(context);
                }, MiddlewareOrder.Configuration);
            }

            static void CreateTracer(CommandLineBuilder builder)
            {
                builder.AddMiddleware(async (context, next) =>
                {
                    // docker run -d --name jaeger -p 6831:6831/udp -p 5778:5778 -p 16686:16686 jaegertracing/all-in-one:1.6
                    using var _ = Sdk.CreateTracerProviderBuilder()
                    .ConfigureResource(r => r.AddService(RpaCommand.ServiceName, serviceVersion: RpaCommand.AssemblyVersion))
                    .AddHttpClientInstrumentation()
                    .AddSource(RpaCommand.AssemblyName)
                    .AddJaegerExporter()
                    .Build();

                    await next(context);
                }, MiddlewareOrder.Configuration);
            }
        }

        internal static CommandLineBuilder RegisterLoggerFactory(this CommandLineBuilder builder)
        {
            return builder.AddMiddleware(async (context, next) =>
            {
                var verbosity = context.ParseResult.GetValueForOption(RpaCommand.VerbosityOption);
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddOpenTelemetry(o =>
                    {
                        o.IncludeFormattedMessage = true;
                        o.AttachLogsToActivityEvent();
                    //    .ConfigureResource(r => r.AddService(RpaCommand.ServiceName, serviceVersion: RpaCommand.AssemblyVersion))
                    })
                    .SetMinimumLevel(verbosity.ToLogLevel());

                    //configuring custom console logging
                    builder.AddConsoleFormatter<CoreMessageConsoleFormatter, SimpleConsoleFormatterOptions>();
                    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());
                    LoggerProviderOptions.RegisterProviderOptions<ConsoleLoggerOptions, ConsoleLoggerProvider>(builder.Services);
                });
                context.BindingContext.AddService(s => loggerFactory);

                await next(context);
            }, MiddlewareOrder.Configuration);
        }

        internal static CommandLineBuilder TrackUsage(this CommandLineBuilder builder)
        {
            return builder.AddMiddleware(async (context, next) =>
            {
                var activitySource = context.BindingContext.GetRequiredService<ActivitySource>();
                using var activity = activitySource.StartActivity(context.ParseResult.CommandResult.GetIssuedCommand());
                foreach (var symbolResult in context.ParseResult.CommandResult.Children)
                    activity?.AddTag(symbolResult.Symbol.Name, string.Join(' ', symbolResult.Tokens));

                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    activity?.RecordException(ex);
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    throw;
                }
            }, MiddlewareOrder.Default);
        }

        internal static string GetIssuedCommand(this CommandResult result)
        {
            ArgumentNullException.ThrowIfNull(result, nameof(result));

            var stack = new Stack<string>();
            var command = result.Command;
            var commandResult = result;
            do
            {
                stack.Push(command.Name);
                commandResult = commandResult?.Parent as CommandResult;
                command = commandResult?.Command;
            } while (command != null);

            return string.Join(' ', stack);
        }
    }

    internal class CoreMessageConsoleFormatter : ConsoleFormatter
    {
        public CoreMessageConsoleFormatter() : base(ConsoleFormatterNames.Simple) { }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
                return;

            if (logEntry.Exception != null)
                textWriter.WriteColoredMessage(logEntry.Exception.Message, ConsoleColor.Black, ConsoleColor.DarkRed);
            else if (!string.IsNullOrEmpty(message))
                textWriter.WriteLine(message);
        }
    }
}