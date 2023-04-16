using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Joba.Xunit
{
    class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper output;

        public XunitLogger(ITestOutputHelper output) => this.output = output;

        IDisposable ILogger.BeginScope<TState>(TState state) => new DummyScope();

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            output.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        }

        class DummyScope : IDisposable
        {
            void IDisposable.Dispose()
            {
            }
        }
    }
}