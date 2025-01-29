using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace TimeTableProvider
{
    public sealed class TimetableClient
    {
        private readonly TimeTableSection _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TimetableClient> _logger;
        private const string CSRFTokenName = "csrftoken";
        private const string SessionIdName = "sessionid";
        private const string ExpiresName = "expires";
        private static Maybe<AuthData> _authDataOrNothing = Maybe<AuthData>.None;

        public TimetableClient(IOptions<TimeTableSection> config, IHttpClientFactory httpClientFactory, ILogger<TimetableClient> logger)
        {
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        internal async Task<Result<string>> LoginAsync()
        {
            if (_authDataOrNothing.HasValue && _authDataOrNothing.Value.IsValid)
                return _authDataOrNothing.Value.AuthCookie;

            _authDataOrNothing = Maybe<AuthData>.None;

            using (var httpClient = _httpClientFactory.CreateClient())
            {
                var loginPageResponse = await httpClient.GetAsync(_config.LoginUrl);
                if (!loginPageResponse.IsSuccessStatusCode)
                    return Result.Failure<string>("Cannot retrieve login page");

                var loginCookiesResult = GetCookies(loginPageResponse);
                if (loginCookiesResult.IsFailure)
                    return loginCookiesResult.Error;

                var cookieString = loginCookiesResult.Value.Single();
                var requestMessageResult = GetLoginRequest(cookieString);
                if (requestMessageResult.IsFailure)
                    return requestMessageResult.Error;

                var loginResponseMessage = await httpClient.SendAsync(requestMessageResult.Value);

                if (loginResponseMessage.StatusCode != HttpStatusCode.Redirect)
                    return Result.Failure<string>($"Unexpected status code after login. Status code: {loginResponseMessage.StatusCode}");

                var authCookiesResult = GetCookies(loginResponseMessage);
                if (authCookiesResult.IsFailure)
                    return authCookiesResult.Error;

                var authCookie = authCookiesResult.Value.Where(c => c.Contains(SessionIdName)).Single();
                var authExpirationDateResult = GetAuthExpirationDate(authCookie);

                if (authExpirationDateResult.IsFailure)
                    return authExpirationDateResult.Error;

                var authDataResult = AuthData.Create(authExpirationDateResult.Value, authCookie);
                if (authCookiesResult.IsFailure)
                    return authCookiesResult.Error;

                _authDataOrNothing = authDataResult.Value;

                return _authDataOrNothing.Value.AuthCookie;
            }
        }

        internal async Task<Result<string>> GetTimeTable(string authCookie, DateTime startWeekDate)
        {
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                var currentWeekUrl = string.Format(_config.TimetableUrl, startWeekDate.ToString("yyyy-MM-dd"));
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, currentWeekUrl);
                requestMessage.Headers.Add("Cookie", authCookie);
                var timeTablePageResponse = await httpClient.SendAsync(requestMessage);
                if (!timeTablePageResponse.IsSuccessStatusCode)
                {
                    return Result.Failure<string>("Cannot get timetable");
                }

                var timeTableResponse = await timeTablePageResponse.Content.ReadAsStringAsync();

                return timeTableResponse;
            }
        }

        private Dictionary<string, string> ParseCookies(string cookieValue)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(cookieValue))
                return result;

            var cookieKeyValuePairs = cookieValue.Split("; ", StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyValue in cookieKeyValuePairs)
            {
                var parsedArray = keyValue.Split("=");
                var parsedValue = parsedArray.Length == 2 ? parsedArray[1] : null;
                result.Add(parsedArray[0], parsedValue);
            }

            return result;
        }

        private Result<IEnumerable<string>> GetCookies(HttpResponseMessage response)
        {
            if (response == null)
                return Result.Failure<IEnumerable<string>>("Response cannot be null");

            if (!response.Headers.TryGetValues("Set-Cookie", out var cookie))
                return Result.Failure<IEnumerable<string>>("Cookie not found in response");

            return Result.Success(cookie);
        }

        private Result<HttpRequestMessage> GetLoginRequest(string cookieString)
        {
            var cookieValues = ParseCookies(cookieString);
            if (!cookieValues.TryGetValue(CSRFTokenName, out var csrfValue))
                return Result.Failure<HttpRequestMessage>($"{CSRFTokenName} not found in cookie");

            var loginFormValues = new Dictionary<string, string>
                    {
                        { "csrfmiddlewaretoken",csrfValue},
                        { "username", _config.Username },
                        { "password", _config.Password },
                        { "|123","|123"}
                    };
            var content = new FormUrlEncodedContent(loginFormValues);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _config.LoginUrl);
            requestMessage.Content = content;
            requestMessage.Headers.Add("Cookie", cookieString);
            requestMessage.Headers.Add("Referer", _config.LoginUrl);

            return requestMessage;
        }

        private Result<DateTime> GetAuthExpirationDate(string authCookie)
        {
            var authCookieValues = ParseCookies(authCookie);

            if (!authCookieValues.TryGetValue(ExpiresName, out var expiresValue))
                return Result.Failure<DateTime>($"{ExpiresName} not found in cookie");

            if (!DateTime.TryParse(expiresValue, out var expiresDate))
                return Result.Failure<DateTime>($"Cannot parse {expiresValue} as datetime");

            if (expiresDate < DateTime.Now)
                return Result.Failure<DateTime>($"{expiresDate} is in past");

            return expiresDate;
        }

        private class AuthData
        {
            internal DateTime ExpirationDate { get; }
            internal string AuthCookie { get; }
            internal bool IsValid => ExpirationDate > DateTime.UtcNow;

            private AuthData(DateTime expirationDate, string authCookie)
            {
                ExpirationDate = expirationDate;
                AuthCookie = authCookie;
            }

            internal static Result<AuthData> Create(DateTime expirationDate, string authCookie)
            {
                if (string.IsNullOrEmpty(authCookie))
                    return Result.Failure<AuthData>("AuthCookie cannot be null or empty");

                var authData = new AuthData(expirationDate.ToUniversalTime(), authCookie);
                return authData;
            }
        }
    }
}
