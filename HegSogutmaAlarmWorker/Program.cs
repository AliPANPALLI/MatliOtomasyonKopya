using HegSogutmaAlarmWorker;
using Microsoft.Extensions.Options;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => options.ServiceName = "HegSogutmaAlarmWorker");
builder.Services.Configure<HegAlarmWorkerOptions>(builder.Configuration.GetSection(HegAlarmWorkerOptions.SectionName));
builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Postgres connection string is not configured.");

    var dbUser = Environment.GetEnvironmentVariable("DB_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    connectionString = connectionString
        .Replace("{DB_USER}", dbUser, StringComparison.Ordinal)
        .Replace("{DB_PASSWORD}", dbPassword, StringComparison.Ordinal);

    return NpgsqlDataSource.Create(connectionString);
});
builder.Services.AddSingleton<IAlarmNotificationSender, SystemAlarmNotificationSender>();
builder.Services.AddHostedService<AlarmEvaluationWorker>();
builder.Services.AddHostedService<AlarmNotificationWorker>();

await builder.Build().RunAsync();
