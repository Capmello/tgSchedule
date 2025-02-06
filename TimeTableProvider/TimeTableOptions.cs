namespace TimeTableProvider
{
    public class TimeTableOptions
    {
        public const string SectionName = "TimeTable";
        public string LoginUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string TimetableUrl { get; set; } = string.Empty;
    }
}
