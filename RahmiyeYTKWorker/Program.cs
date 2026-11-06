using PoultryHouseScada.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using RahmiyeYTKWorker;
using RahmiyeYTKWorker.Core;
using SharedApp.Infrastructure;

IHost host = Host.CreateDefaultBuilder(args)
      .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Worker servisini Scoped olarak ekliyoruz
        services.AddHostedService<RahmiyeYtkForRaporWorker>();

        var connectionString = hostContext.Configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string is not configured.");
        }

        services.AddDbContext<KumesScadaDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        var sharedConnectionString = hostContext.Configuration.GetConnectionString("SharedSqlServer");
        if (string.IsNullOrEmpty(sharedConnectionString))
        {
            throw new InvalidOperationException("Shared Connection string is not configured.");
        }
        services.AddDbContext<SharedDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(sharedConnectionString);
        }, ServiceLifetime.Scoped);
        //services.AddSingleton<AlarmCacheService>();
        //services.AddScoped<SqlConnection>(provider => new SqlConnection(connectionString));
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();