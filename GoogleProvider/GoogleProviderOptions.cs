namespace GoogleProvider
{
    public sealed class GoogleProviderOptions
    {
        public const string SectionName = "GoogleProvider";
        public string GoogleServiceAccountFileName { get; set; } = string.Empty;
        public string CalendarName { get; set; } = string.Empty;
    }
}
