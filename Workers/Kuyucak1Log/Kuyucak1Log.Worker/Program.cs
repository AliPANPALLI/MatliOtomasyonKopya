using Kuyucak1Log.Worker;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;
using System.Data.SqlClient;
using Kuyucak1Log.Infrastructure.Caching;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configuration => configuration.AddEnvironmentPlaceholders())
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var connectionString =
            hostContext.Configuration.GetConnectionString("SqlServer");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SqlServer connection string is not configured.");
        }

        var sharedConnectionString =
            hostContext.Configuration.GetConnectionString("SharedSqlServer");

        if (string.IsNullOrWhiteSpace(sharedConnectionString))
        {
            throw new InvalidOperationException(
                "SharedSqlServer connection string is not configured.");
        }

        services.AddMemoryCache();

        services.AddDbContext<KumesScadaDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddDbContext<SharedDbContext>(options =>
        {
            options.UseSqlServer(sharedConnectionString);
        });

        services.AddScoped<SqlConnection>(_ =>
            new SqlConnection(connectionString));

        services.AddSingleton<CacheService>();
        services.AddSingleton<EmailSender>();
        services.AddHostedService<Kuyucak1TesisLogNodeRedWorker>();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
