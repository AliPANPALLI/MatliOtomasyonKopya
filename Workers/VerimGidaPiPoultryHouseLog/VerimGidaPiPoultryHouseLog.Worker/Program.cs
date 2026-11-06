using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;
using System.Data.SqlClient;
using VerimGidaPiPoultryHouseLog.Worker;
using VerimGidaPiPoultryHouseLog.Worker.Core;

IHost host = Host.CreateDefaultBuilder(args)
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

        /*
         * AlarmCacheService singleton kalabilir.
         * Artýk DbContext'i constructor üzerinden almýyor;
         * ihtiyaç duyduðu her iþlemde kendi scope'unu oluþturuyor.
         */
        services.AddSingleton<AlarmCacheService>();
        services.AddSingleton<EmailSender>();

        /*
         * HostedService singleton olarak çalýþýr.
         * Bu nedenle worker constructor'ýna scoped DbContext veya
         * scoped SqlConnection verilmemelidir.
         */
        services.AddHostedService<VerimGidaPiKumesLogNodeRedWorker>();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
