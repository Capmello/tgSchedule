using CSharpFunctionalExtensions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace GoogleProvider
{
    public sealed class CalendarClient
    {
        private static CalendarService _calendarService;
        private Result<CalendarService> CalendarServiceInstance
        {
            get
            {
                if (_calendarService == null)
                {
                    var createCalendarServiceResult = CreateCalendarService();
                    if (createCalendarServiceResult.IsFailure)
                        return Result.Failure<CalendarService>(createCalendarServiceResult.Error);
                    else
                    {
                        _calendarService = createCalendarServiceResult.Value;
                    }
                }

                return _calendarService;
            }
        }
        private readonly GoogleProviderOptions _config;

        public CalendarClient(IOptions<GoogleProviderOptions> config)
        {
            _config = config.Value;
        }

        public async Task<Result<IReadOnlyCollection<Event>>> GetEvents(DateTime startDate, DateTime endDate)
        {
            if (CalendarServiceInstance.IsFailure)
                return Result.Failure<IReadOnlyCollection<Event>>(CalendarServiceInstance.Error);

            var eventsRequest = CalendarServiceInstance.Value.Events.List(_config.CalendarName);
            eventsRequest.SingleEvents = true;
            eventsRequest.TimeMinDateTimeOffset = startDate;
            eventsRequest.TimeMaxDateTimeOffset = endDate;

            try
            {
                var eventsResponse = await eventsRequest.ExecuteAsync();
                var events = eventsResponse.Items ?? Enumerable.Empty<Event>();

                return events.ToList();
            }
            catch (Exception ex)
            {
                return Result.Failure<IReadOnlyCollection<Event>>(ex.ToString());
            }
        }

        public async Task<Result> UpdateEvent(List<Event> lessonEvents)
        {
            if (CalendarServiceInstance.IsFailure)
                return Result.Failure<IReadOnlyCollection<Event>>(CalendarServiceInstance.Error);

            foreach (var lessonEvent in lessonEvents)
            {
                try
                {
                    var updateRequest = CalendarServiceInstance.Value.Events.Update(lessonEvent, _config.CalendarName, lessonEvent.Id);

                    var resp = await updateRequest.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    var str = $"Start Time: {lessonEvent.Start.DateTimeDateTimeOffset}, Description: {lessonEvent.Description}, Summary: {lessonEvent.Summary}, Location {lessonEvent.Location}";
                    return Result.Failure($"Cannot update event. {str}");
                }
            }

            return Result.Success();
        }

        private Result<CalendarService> CreateCalendarService()
        {
            var filePath = _config.GoogleServiceAccountJsonPath;
            if (!File.Exists(filePath))
                return Result.Failure<CalendarService>($"File not found. File path: '{filePath}'");

            GoogleCredential credential = GoogleCredential.FromFile(filePath)
                .CreateScoped(CalendarService.Scope.Calendar);

            var result = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential
            });

            return result;
        }
    }
}
