namespace Joba.IBM.RPA.Cli
{
    internal static class Extensions
    {
        public static IRpaClient CreateClient(this Region region) =>
            new RpaClient(HttpRpaFactory.Create(region.ApiUrl));

        public static IRpaClient CreateClient(this Project project) =>
            project.CurrentEnvironment.Region.CreateClient();
    }
}
