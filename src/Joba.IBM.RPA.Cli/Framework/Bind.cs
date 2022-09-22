using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Joba.IBM.RPA.Cli
{
    /// <summary>
    /// Taken from https://github.com/dotnet/command-line-api/issues/1750#issuecomment-1152707726
    /// </summary>
    internal static class Bind
    {
        internal static BinderBase<T> FromServiceProvider<T>() => ServiceProviderBinder<T>.Instance;
        internal static BinderBase<ILogger<T>> FromLogger<T>() => new LoggerBinder<T>();

        sealed class ServiceProviderBinder<T> : BinderBase<T>
        {
            private static readonly ServiceProviderBinder<T> instance = new();

            public static ServiceProviderBinder<T> Instance => instance;

            protected override T GetBoundValue(BindingContext bindingContext) => bindingContext.GetService<T>();
        }

        sealed class LoggerBinder<T> : BinderBase<ILogger<T>>
        {
            protected override ILogger<T> GetBoundValue(BindingContext bindingContext)
            {
                var factory = bindingContext.GetRequiredService<ILoggerFactory>();
                return factory.CreateLogger<T>();
            }
        }
    }
}
