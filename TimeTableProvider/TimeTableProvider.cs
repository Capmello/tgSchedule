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

        public async Task<List<Result<Timesheet>>> GetTimetables(DateTime[] weekStartDates)
        {
            var authCookieResult = await _timetableClient.LoginAsync();
            if (authCookieResult.IsFailure)
                return new List<Result<Timesheet>> { Result.Failure<Timesheet>(authCookieResult.Error) };

            var authCookie = authCookieResult.Value;

            var result = new List<Result<Timesheet>>();
            foreach (var weekStartDate in weekStartDates)
            {
                var timeTableHtmlResult = await _timetableClient.GetTimeTable(authCookie, weekStartDate);
                if (timeTableHtmlResult.IsFailure)
                {
                    result.Add(Result.Failure<Timesheet>(authCookieResult.Error));
                    continue;
                }

                var timeTableHtml = timeTableHtmlResult.Value;

                var timesheetList = TimetableHtmlParser.ParseTimeTable(timeTableHtml);
                result.AddRange(timesheetList);
            }

            return result;
        }
    }
}
