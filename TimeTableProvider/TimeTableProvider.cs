using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace TimeTableProvider
{
    public sealed class TimeTableProvider
    {
        private readonly TimetableClient _timetableClient;

        public TimeTableProvider(TimetableClient timetableClient)
        {
            _timetableClient = timetableClient;
        }

        public async Task<List<Result<Timesheet>>> GetTimetables(DateTime startDate, DateTime endDate)
        {
            var authCookieResult = await _timetableClient.LoginAsync();
            if (authCookieResult.IsFailure)
                return new List<Result<Timesheet>> { Result.Failure<Timesheet>(authCookieResult.Error) };

            var authCookie = authCookieResult.Value;

            var result = new List<Result<Timesheet>>();
            var weekStartDates = GetWeekStartDates(startDate, endDate);
            foreach (var weekStartDate in weekStartDates)
            {
                var timeTableHtmlResult = await _timetableClient.GetTimeTable(authCookie, weekStartDate);
                if (timeTableHtmlResult.IsFailure)
                {
                    result.Add(Result.Failure<Timesheet>(timeTableHtmlResult.Error));
                    continue;
                }

                var timeTableHtml = timeTableHtmlResult.Value;

                var timesheetList = TimetableHtmlParser.ParseTimeTable(timeTableHtml);
                result.AddRange(timesheetList);
            }

            return result;
        }

        private IEnumerable<DateTime> GetWeekStartDates(DateTime startDate, DateTime endDate)
        {
            int days = startDate.DayOfWeek - DayOfWeek.Monday;
            var currentWeekStartDate = startDate.AddDays(-days);

            var periodEndDate = currentWeekStartDate.AddDays(7);
            var result = new List<DateTime> { currentWeekStartDate};
            while (periodEndDate < endDate)
            {
                result.Add(periodEndDate);
                periodEndDate = periodEndDate.AddDays(7);
            }

            return result;
        }
    }
}
