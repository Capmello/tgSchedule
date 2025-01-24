namespace TimeTableProvider
{
    public sealed class TimeTableProvider
    {
        private readonly TimetableClient _timetableClient;

        public TimeTableProvider(TimetableClient timetableClient)
        {
            _timetableClient = timetableClient;
        }
        public async Task GetTimetable()
        {

            await _timetableClient.LoginAsync();
        }
    }
}
