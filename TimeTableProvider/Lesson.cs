using CSharpFunctionalExtensions;

namespace TimeTableProvider
{
    public sealed class Lesson : ValueObject
    {
        public string Name { get; }
        public string? HomeWork { get; }
        public int Number { get; }

        private Lesson(int number, string name, string? homeWork)
        {
            Number = number;
            Name = name;
            HomeWork = homeWork;
        }

        public static Result<Lesson> Create(int number, string name, string? homeWork)
        {
            var lessonName = (name ?? string.Empty).TrimStart().TrimEnd();
            
            if (number < 0)
                return Result.Failure<Lesson>("Order cannot be less than 0");

            if (string.IsNullOrEmpty(lessonName))
                return Result.Failure<Lesson>("Lesson name cannot be null or empty");

            return new Lesson(number, lessonName, homeWork);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }
    }
}
