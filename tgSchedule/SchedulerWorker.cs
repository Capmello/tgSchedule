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
            /*var result = */await _timeTableProvider.GetTimetable();
            
            
            //get schedule
            //if ok try to update google calendar
        }
    }
}
