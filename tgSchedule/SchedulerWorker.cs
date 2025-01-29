using Microsoft.Extensions.Logging;
using System.Text;
using TimeTableProvider;

namespace tgSchedule
{
    internal class SchedulerWorker
    {
        private readonly ILogger<SchedulerWorker> _logger;
        private readonly TimeTableProvider.TimeTableProvider _timeTableProvider;

        public SchedulerWorker(ILogger<SchedulerWorker> logger, TimeTableProvider.TimeTableProvider timeTableProvider)
        {
            _logger = logger;
            _timeTableProvider = timeTableProvider;
        }

        public async Task DoWork()
        {
            var now = DateTime.Now;
            int days = now.DayOfWeek - DayOfWeek.Monday;
            var currentWeekStartDate = now.AddDays(-days);
            var weeks = new[] { currentWeekStartDate, currentWeekStartDate.AddDays(7) };

            var timeTableResults = await _timeTableProvider.GetTimetables(weeks);
            var sb = new StringBuilder();
            foreach (var result in timeTableResults)
            {
                if (result.IsFailure)
                    sb.AppendLine(result.Error);
                else
                {
                    var timeTable = result.Value;
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

            //get schedule
            //if ok try to update google calendar
        }
    }
}
