using System.CommandLine.Binding;

namespace Joba.IBM.RPA
{
    internal static class Bind
    {
        public static ServiceProviderBinder<T> FromServiceProvider<T>() => ServiceProviderBinder<T>.Instance;

        internal sealed class ServiceProviderBinder<T> : BinderBase<T>
        {
            private static readonly ServiceProviderBinder<T> instance = new();

            public static ServiceProviderBinder<T> Instance => instance;

            protected override T GetBoundValue(BindingContext bindingContext) => (T)bindingContext.GetService(typeof(T));
        }
    }
}
