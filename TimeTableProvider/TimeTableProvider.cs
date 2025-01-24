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

            var authCookie = await _timetableClient.LoginAsync();
            await _timetableClient.GetTimeTable(authCookie.Value);
        }
    }
}
