namespace Joba.IBM.RPA.Cli
{
    class FetchCommand : Command
    {
        public FetchCommand() : base("fetch", "Fetches the project files")
        {
            var fileName = new Argument<string>("fileName", () => string.Empty, "The specific wal file name");
            AddArgument(fileName);

            this.SetHandler(HandleAsync, fileName,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string fileName, Project project, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var fetchService = new FetchService(project);

            if (string.IsNullOrEmpty(fileName))
            {
                await fetchService.AllAsync(cancellation);
                var command = new StatusCommand();
                command.Handle(project);
            }
            else
                await fetchService.OneAsync(fileName, cancellation);
        }
    }
}