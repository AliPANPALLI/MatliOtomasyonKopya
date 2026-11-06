using PoultryHouseScada.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SharedApp.Infrastructure;
using VerimGidaYTKWorker;
using VerimGidaYTKWorker.Core;

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
        services.AddHostedService<VerimGidaYtkForRaporWorker>();

        var connectionString = hostContext.Configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string is not configured.");
        }
        var sharedConnectionString = hostContext.Configuration.GetConnectionString("SharedSqlServer");
        if (string.IsNullOrEmpty(sharedConnectionString))
        {
            throw new InvalidOperationException("Shared Connection string is not configured.");
        }
        services.AddDbContext<KumesScadaDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);
        services.AddDbContext<SharedDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(sharedConnectionString);
        }, ServiceLifetime.Scoped);
        services.AddScoped<AlarmCacheService>();

        services.AddTransient<SqlConnection>(provider => new SqlConnection(connectionString));
        services.AddMemoryCache();

    })
    .UseWindowsService()
    .Build();

await host.RunAsync();