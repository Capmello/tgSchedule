using CSharpFunctionalExtensions;
using HtmlAgilityPack;
using System.Globalization;

namespace TimeTableProvider
{
    public sealed class TimetableHtmlParser
    {
        internal static Result<List<Timesheet>> ParseTimeTable(string html)
        {
            var tableNodesResult = GetTableNodes(html);

            if (tableNodesResult.IsFailure)
                return Result.Failure<List<Timesheet>>(tableNodesResult.Error);

            var tableNodes = tableNodesResult.Value;
            var result = new List<Timesheet>();

            foreach (var tableNode in tableNodes)
            {
                var timesheetResult = ConvertTableToTimesheet(tableNode);
                if (timesheetResult.IsFailure)
                    return Result.Failure<List<Timesheet>>(timesheetResult.Error);

                result.Add(timesheetResult.Value);
            }
            return result;
        }

        private static Result<IEnumerable<HtmlNode>> GetTableNodes(string html)
        {
            if (string.IsNullOrEmpty(html))
                return Result.Failure<IEnumerable<HtmlNode>>("Html cannot be null or empty");

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var weekTimeTables = htmlDoc.DocumentNode.SelectNodes(".//table[starts-with(@id, 'db_table_')]");
            Maybe<HtmlNode[]> tableNodesOrNothing = Maybe<HtmlNode[]>.None;
            if (weekTimeTables != null)
                //tables could be duplicated
                tableNodesOrNothing = weekTimeTables.DistinctBy(t => t.GetAttributeValue("id", "N/A")).ToArray();

            if (tableNodesOrNothing.HasNoValue)
                return Result.Failure<IEnumerable<HtmlNode>>($"No timetables found in {html}");

            return tableNodesOrNothing.Value;
        }

        private static Result<Timesheet> ConvertTableToTimesheet(HtmlNode table)
        {
            var idVal = table.Attributes["id"].Value;
            var datePart = idVal.Replace("db_table_", string.Empty);
            if (!DateTime.TryParseExact(datePart, "dd.MM.yy", null, DateTimeStyles.None, out var timesheetDate))
                return Result.Failure<Timesheet>($"Cannot parse datetime from table id {idVal}");

            var timesheet = Timesheet.Create(timesheetDate, Maybe<List<Lesson>>.None).Value;

            var rows = table.SelectNodes(".//tr");

            foreach (var row in rows)
            {
                var tdCollection = row.SelectNodes(".//td");
                if (tdCollection == null || !tdCollection.Any())
                    continue;

                var lessonTextRaw = row.SelectSingleNode(".//td[starts-with(@class,'lesson')]//span").InnerText;
                var normalizedLessonText = lessonTextRaw.NormalizeString();
                var firstDotPosition = normalizedLessonText.IndexOf(".");
                var numberPart = normalizedLessonText.Substring(0, firstDotPosition);
                var lessonNamePart = normalizedLessonText.Substring(firstDotPosition + 1);
                var homeWorkRaw = row.SelectSingleNode(".//td[@class='ht']//div[@class='ht-text']")?.InnerText ?? string.Empty;
                var homeWork = homeWorkRaw.NormalizeString();
                var order = int.Parse(numberPart);
                if (!string.IsNullOrEmpty(lessonNamePart))
                {
                    var lessonResult = Lesson.Create(order, lessonNamePart, homeWork);
                    if (lessonResult.IsFailure)
                        return Result.Failure<Timesheet>(lessonResult.Error);

                    var addLessonResult = timesheet.AddLesson(lessonResult.Value);

                    if (addLessonResult.IsFailure)
                        return Result.Failure<Timesheet>(addLessonResult.Error);
                }
            }

            return timesheet;
        }
    }
}
