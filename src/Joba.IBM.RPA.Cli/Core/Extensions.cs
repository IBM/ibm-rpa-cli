namespace Joba.IBM.RPA.Cli
{
    internal static class Extensions
    {
        public static IRpaClient CreateClient(this Region region) =>
            new RpaClient(HttpRpaFactory.Create(region.ApiUrl));
    }
}
