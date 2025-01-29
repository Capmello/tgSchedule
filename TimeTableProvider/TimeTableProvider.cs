using Microsoft.Extensions.Logging;
using System.Text;

namespace TimeTableProvider
{
    public sealed class TimeTableProvider
    {
        private readonly TimetableClient _timetableClient;
        private readonly ILogger<TimeTableProvider> _logger;

        public TimeTableProvider(TimetableClient timetableClient, ILogger<TimeTableProvider> logger)
        {
            _timetableClient = timetableClient;
            _logger = logger;
        }

        public async Task GetTimetable(DateTime[] weekStartDates)
        {

            var authCookieResult = await _timetableClient.LoginAsync();
            var authCookie = authCookieResult.Value;
            foreach (var weekStartDate in weekStartDates)
            {
                var timeTableHtmlResult = await _timetableClient.GetTimeTable(authCookie, weekStartDate);
                var timeTableHtml = timeTableHtmlResult.Value;

                var timesheetList = TimetableHtmlParser.ParseTimeTable(timeTableHtml);

                var sb = new StringBuilder();
                foreach (var timesheet in timesheetList.Value)
                {
                    sb.AppendLine($"Day: {timesheet.Date}");
                    if (timesheet.LessonsOrNothing.HasNoValue)
                        sb.AppendLine($"No lessons");
                    else
                    {
                        foreach (var x in timesheet.LessonsOrNothing.Value)
                        {
                            var str = $"{x.Order}.{x.Name} Homework: {x.HomeWork}";
                            sb.AppendLine(str);
                        }
                    }
                }
                _logger.LogInformation(sb.ToString());
            }
        }
    }
}
