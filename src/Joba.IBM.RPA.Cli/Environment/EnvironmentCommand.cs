namespace Joba.IBM.RPA.Cli
{
    [RequiresProject]
    internal partial class EnvironmentCommand : Command
    {
        public static readonly string CommandName = "env";
        internal EnvironmentCommand() : base(CommandName, "Manages environments")
        {
            AddCommand(new NewEnvironmentCommand());
        }
    }

    class RemoteOptionsBinder : BinderBase<RemoteOptions>
    {
        private readonly Argument<string> aliasArgument;
        private readonly Option<string?> urlOption;
        private readonly Option<string?> regionOption;
        private readonly Option<string?> userNameOption;
        private readonly Option<int?> tenantCodeOption;
        private readonly Option<string?> passwordOption;

        public RemoteOptionsBinder(Argument<string> aliasArgument, Option<string?> urlOption,
            Option<string?> regionOption, Option<string?> userNameOption, Option<int?> tenantCodeOption, Option<string?> passwordOption)
        {
            this.aliasArgument = aliasArgument;
            this.urlOption = urlOption;
            this.regionOption = regionOption;
            this.userNameOption = userNameOption;
            this.tenantCodeOption = tenantCodeOption;
            this.passwordOption = passwordOption;
        }

        protected override RemoteOptions GetBoundValue(BindingContext bindingContext)
        {
            return new RemoteOptions(
                bindingContext.ParseResult.GetValueForArgument(aliasArgument),
                new ServerAddress(bindingContext.ParseResult.GetValueForOption(urlOption)),
                bindingContext.ParseResult.GetValueForOption(regionOption),
                bindingContext.ParseResult.GetValueForOption(userNameOption),
                bindingContext.ParseResult.GetValueForOption(tenantCodeOption),
                bindingContext.ParseResult.GetValueForOption(passwordOption));
        }
    }
}