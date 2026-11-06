using HegCoolingAlarm.Worker;
using Microsoft.Extensions.Options;
using Npgsql;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentPlaceholders();

builder.Services.AddWindowsService(options => options.ServiceName = "HegSogutmaAlarmWorker");
builder.Services.Configure<HegAlarmWorkerOptions>(builder.Configuration.GetSection(HegAlarmWorkerOptions.SectionName));
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Postgres connection string is not configured.");

    return NpgsqlDataSource.Create(connectionString);
});
builder.Services.AddSingleton<IAlarmNotificationSender, SystemAlarmNotificationSender>();
builder.Services.AddHostedService<AlarmEvaluationWorker>();
builder.Services.AddHostedService<AlarmNotificationWorker>();

await builder.Build().RunAsync();
