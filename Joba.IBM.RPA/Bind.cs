using System.CommandLine;
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

    internal class AccountBinder : BinderBase<Account>
    {
        private readonly Option<int> tenantOption;
        private readonly Option<string> userNameOption;
        private readonly Option<string> passwordOption;

        public AccountBinder(Option<int> tenantOption, Option<string> userNameOption, Option<string> passwordOption)
        {
            this.tenantOption = tenantOption;
            this.userNameOption = userNameOption;
            this.passwordOption = passwordOption;
        }

        protected override Account GetBoundValue(BindingContext bindingContext)
        {
            return new Account(
                bindingContext.ParseResult.GetValueForOption(tenantOption),
                bindingContext.ParseResult.GetValueForOption(userNameOption),
                bindingContext.ParseResult.GetValueForOption(passwordOption));
        }
    }

    record struct Account(int TenantCode, string UserName, string Password);
}
