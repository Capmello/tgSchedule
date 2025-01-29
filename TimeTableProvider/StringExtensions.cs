namespace TimeTableProvider
{
    internal static class StringExtensions
    {
        internal static string NormalizeString(this string str)
        {
            if (str == null)
                return str;

            return str.TrimStart().TrimEnd().Replace("  ", " ").Replace(Environment.NewLine, string.Empty);
        }
    }
}
