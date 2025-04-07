using CSharpFunctionalExtensions;
using GoogleProvider;
using Microsoft.Extensions.Logging;
using System.Text;
using TimeTableProvider;

namespace tgSchedule
{
    internal class SchedulerWorker
    {
        private readonly ILogger<SchedulerWorker> _logger;
        private readonly TimeTableProvider.TimeTableProvider _timeTableProvider;
        private readonly CalendarClient _calendarClient;

        public SchedulerWorker(
            ILogger<SchedulerWorker> logger, TimeTableProvider.TimeTableProvider timeTableProvider, CalendarClient calendarClient)
        {
            _logger = logger;
            _timeTableProvider = timeTableProvider;
            _calendarClient = calendarClient;
        }

        public async Task DoWork()
        {
            var startDate = DateTime.Now;
            var endDate = startDate.Date.AddDays(14);

            var timesheetTask = GetTimesheet(startDate, endDate);
            var calendarEventsTask = _calendarClient.GetEvents(startDate, endDate);

            await Task.WhenAll(timesheetTask, calendarEventsTask);

            var events = calendarEventsTask.Result.Value;
            var timesheets = timesheetTask.Result;

            var sb = new StringBuilder();
            var eSb = new StringBuilder();

            foreach (var ev in events)
            {
                var str = $"Start Time: {ev.Start.DateTimeDateTimeOffset}, Description: {ev.Description}, Summary: {ev.Summary}, Location {ev.Location}";
                sb.AppendLine(str);
            }

            _logger.LogInformation(eSb.ToString());
            var lessonsToUpdate = new List<Google.Apis.Calendar.v3.Data.Event>();


            foreach (var timesheet in timesheets)
            {
                var dtTimesheet = timesheet.Date;
                if (timesheet.LessonsOrNothing.HasValue)
                {
                    var correspondingEvents = events.Where(e => e.Start.DateTimeDateTimeOffset.HasValue && e.Start.DateTimeDateTimeOffset.Value.DateTime.Date == dtTimesheet).ToArray();
                    foreach (var lesson in timesheet.LessonsOrNothing.Value)
                    {
                        var lessonEvent = correspondingEvents.FirstOrDefault(e => string.Equals(e.Summary, lesson.Name, StringComparison.OrdinalIgnoreCase));
                        if (lessonEvent != null && !string.IsNullOrWhiteSpace(lesson.HomeWork))
                        {
                            var homeWork = lesson.HomeWork;
                            var existingLocation = lessonEvent.Location ?? string.Empty;
                            //TODO after update never will be the same. Necessary to use contain or end with.
                            var isSame = !string.IsNullOrEmpty(existingLocation) && string.Equals(existingLocation, homeWork, StringComparison.OrdinalIgnoreCase);
                            if (isSame)
                                continue;

                            if (existingLocation.EndsWith(homeWork))
                                continue;

                            if (!string.IsNullOrEmpty(existingLocation))
                                homeWork = $"{existingLocation}|{homeWork}";

                            lessonEvent.Location = homeWork;
                            lessonsToUpdate.Add(lessonEvent);
                        }
                    }

                }

            }
            await _calendarClient.UpdateEvent(lessonsToUpdate);

        }

        private async Task<IReadOnlyCollection<Timesheet>> GetTimesheet(DateTime startDate, DateTime endDate)
        {
            var timeTableResults = await _timeTableProvider.GetTimetables(startDate, endDate);
            var sb = new StringBuilder();
            foreach (var timeTableResult in timeTableResults)
            {
                if (timeTableResult.IsFailure)
                    sb.AppendLine(timeTableResult.Error);
                else
                {
                    var timeTable = timeTableResult.Value;
                    sb.AppendLine(timeTable.Date.ToShortDateString());
                    if (timeTable.LessonsOrNothing.HasValue)
                    {
                        var orderedLessons = timeTable.LessonsOrNothing.Value.OrderBy(l => l.Number);
                        foreach (var lesson in orderedLessons)
                        {
                            sb.AppendLine($"{lesson.Number}.{lesson.Name}. Homework: {lesson.HomeWork ?? "-"}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("No lessons");
                    }

                }
            }
            _logger.LogInformation(sb.ToString());

            var result = timeTableResults.Where(t => t.IsSuccess).Select(t => t.Value).ToList();

            return result;
        }
    }
}
