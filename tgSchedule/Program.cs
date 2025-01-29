// See https://aka.ms/new-console-template for more information
using Hangfire;
using Hangfire.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tgSchedule;
using TimeTableProvider;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(builder => builder.AddConsole());
serviceCollection.AddHangfire(config =>
        config.UseInMemoryStorage(new InMemoryStorageOptions
        {
            IdType = InMemoryStorageIdType.Guid
        })
        );
serviceCollection.AddTransient<SchedulerWorker>();
serviceCollection.AddTransient<TimeTableProvider.TimeTableProvider>();
serviceCollection.AddTransient<TimetableClient>();
serviceCollection.Configure<TimeTableSection>(config.GetSection(TimeTableSection.SectionName));
serviceCollection.AddHttpClient().ConfigureHttpClientDefaults(b =>
                 b.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler() { UseCookies = false, AllowAutoRedirect = false }));

var serviceProvider = serviceCollection.BuildServiceProvider();

ScheduleReccuringTasks(serviceProvider.GetRequiredService<IRecurringJobManager>());

var options = new BackgroundJobServerOptions { SchedulePollingInterval = new TimeSpan(0, 0, 1), WorkerCount = 1 };
using (var server = new BackgroundJobServer(options))
{
    Console.ReadLine();
}



void ScheduleReccuringTasks(IRecurringJobManager recurringJobManager)
{
    recurringJobManager.AddOrUpdate<SchedulerWorker>("ScheduleUpdate", sw => sw.DoWork(), "*/5 * * * *");
}
