using CSharpFunctionalExtensions;

namespace TimeTableProvider
{
    public sealed class Timesheet
    {
        public DateTime Date { get; }
        public Maybe<List<Lesson>> LessonsOrNothing { get; private set; }

        private Timesheet(DateTime date, Maybe<List<Lesson>> lessonsOrNothing)
        {
            Date = date.Date;
            LessonsOrNothing = lessonsOrNothing;
        }

        public static Result<Timesheet> Create(DateTime date, Maybe<List<Lesson>> lessonsOrNothing)
        {
            return new Timesheet(date, lessonsOrNothing);
        }

        public Result AddLesson(Lesson lesson)
        {
            if (LessonsOrNothing.HasNoValue)
                LessonsOrNothing = new List<Lesson>();

            if (LessonsOrNothing.Value.Any(l => l.Name == lesson.Name && l.Order == lesson.Order))
                return Result.Failure("Cannot add duplicate lesson");

            LessonsOrNothing.Value.Add(lesson);
            return Result.Success();
        }

    }
}
