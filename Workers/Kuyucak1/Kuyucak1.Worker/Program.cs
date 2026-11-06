using Kuyucak1.Worker;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;
using System.Data.SqlClient;
using Kuyucak1.Infrastructure.Caching;


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
         * Artık DbContext'i constructor üzerinden almıyor;
         * ihtiyaç duyduğu her işlemde kendi scope'unu oluşturuyor.
         */
        services.AddSingleton<AlarmCacheService>();
        services.AddSingleton<EmailSender>();

        /*
         * HostedService singleton olarak çalışır.
         * Bu nedenle worker constructor'ına scoped DbContext veya
         * scoped SqlConnection verilmemelidir.
         */
        services.AddHostedService<Kuyucak1KumesNodeRedWorker>();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
