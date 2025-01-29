using CSharpFunctionalExtensions;
using System.IO;

namespace TimeTableProvider
{
    public sealed class Lesson : ValueObject
    {
        public string Name { get; }
        public string? HomeWork { get; }
        public int Order { get; }

        private Lesson(int order, string name, string? homeWork)
        {
            Order = order;
            Name = name;
            HomeWork = homeWork;
        }

        public static Result<Lesson> Create(int order, string name, string? homeWork)
        {
            var lessonName = (name ?? string.Empty).TrimStart().TrimEnd();
            
            if (order < 0)
                return Result.Failure<Lesson>("Order cannot be less than 0");

            if (string.IsNullOrEmpty(lessonName))
                return Result.Failure<Lesson>("Lesson name cannot be null or empty");

            return new Lesson(order, lessonName, homeWork);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
        }
    }
}
