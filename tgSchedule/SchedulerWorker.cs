using Microsoft.Extensions.Logging;

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
            /*var result = */
            await _timeTableProvider.GetTimetable(weeks);
            
            
            //get schedule
            //if ok try to update google calendar
        }
    }
}
