namespace Joba.IBM.RPA.Cli
{
    class PropertyOptionsBinder : BinderBase<PropertyOptions>
    {
        private readonly Option<IEnumerable<string>?> option;

        public PropertyOptionsBinder(Option<IEnumerable<string>?> option) => this.option = option;

        protected override PropertyOptions GetBoundValue(BindingContext bindingContext)
        {
            var raw = bindingContext.ParseResult.GetValueForOption(option);
            return raw == null ? new PropertyOptions() : PropertyOptions.Parse(raw.ToArray());
        }
    }
}
