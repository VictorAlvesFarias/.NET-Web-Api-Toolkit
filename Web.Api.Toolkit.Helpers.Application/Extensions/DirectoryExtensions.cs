namespace Web.Api.Toolkit.Helpers.Application.Extensions
{
    public static class DirectoryExtensions
    {
        public static long GetDirectorySize(this DirectoryInfo directory)
        {
            if (!directory.Exists)
                return 0;

            return directory
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
    }
}
