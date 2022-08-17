namespace Joba.IBM.RPA.Cli
{
    class PullCommand : Command
    {
        public PullCommand() : base("pull", "Pull the project files")
        {
            var fileName = new Argument<string>("fileName", () => string.Empty, "The specific wal file name");
            AddArgument(fileName);

            this.SetHandler(HandleAsync, fileName,
                Bind.FromServiceProvider<Project>(),
                Bind.FromServiceProvider<Environment>(),
                Bind.FromServiceProvider<InvocationContext>());
        }

        private async Task HandleAsync(string fileName, Project project, Environment environment, InvocationContext context)
        {
            var cancellation = context.GetCancellationToken();
            var fetchService = new PullService(project, environment);

            if (string.IsNullOrEmpty(fileName))
            {
                await fetchService.AllAsync(cancellation);
                var command = new StatusCommand();
                command.Handle(project, environment);
            }
            else
                await fetchService.OneAsync(fileName, cancellation);
        }
    }
}