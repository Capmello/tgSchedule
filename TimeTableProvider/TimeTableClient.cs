using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using System.Net;

namespace TimeTableProvider
{
    public sealed class TimetableClient
    {
        private readonly TimeTableSection _config;

        public TimetableClient(IOptions<TimeTableSection> config)
        {
            _config = config.Value;
        }

        internal async Task LoginAsync()
        {
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer, UseCookies = true })
            using (var httpClient = new HttpClient(handler))
            {
                var loginPageResponse = await httpClient.GetAsync(_config.LoginUrl);
                if (!loginPageResponse.IsSuccessStatusCode)
                {
                    //TODO: return page is unavailable
                }
                //csrf from cookie?
                var loginPageString = await loginPageResponse.Content.ReadAsStringAsync();
                HtmlDocument loginPageDocument = new HtmlDocument();
                loginPageDocument.LoadHtml(loginPageString);
                var hiddenCSRFMiddlewareToken = loginPageDocument.DocumentNode.SelectSingleNode($"//input[@type='hidden' and @name='csrfmiddlewaretoken']");
                var csrfValue = hiddenCSRFMiddlewareToken?.GetAttributeValue("value", null);
                if (string.IsNullOrEmpty(csrfValue))
                {
                    //TODO: return token not found
                }
                var values = new Dictionary<string, string>
                {
                    { "csrfmiddlewaretoken",csrfValue},
                    { "username", _config.Username },
                    { "password", _config.Password },
                    { "|123","|123"}
                };
                var content = new FormUrlEncodedContent(values);
                httpClient.DefaultRequestHeaders.Add("Referer", _config.LoginUrl);
                var responseMessage = await httpClient.PostAsync(_config.LoginUrl, content);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    //TODO: return cannot login
                }
                var response = await responseMessage.Content.ReadAsStringAsync();

                //get timetable
                var timeTablePageResponse = await httpClient.GetAsync(_config.TimetableUrl);

                if (!timeTablePageResponse.IsSuccessStatusCode)
                {
                    //TODO: return cannot login
                }
                var timeTableResponse = await timeTablePageResponse.Content.ReadAsStringAsync();
                //should contain <div class="db_period">
            }
        }

        internal void GetTimeTable()
        {

        }
    }
}
