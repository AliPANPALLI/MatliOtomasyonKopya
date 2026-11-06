using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;
using WaterLineScada.Infrastructure;
using WaterMonitoring.Worker;
using WaterMonitoring.Worker.Core;
using System.Data.SqlClient;

IHost host = Host.CreateDefaultBuilder(args)
     .ConfigureWebHostDefaults(webBuilder =>
     {
         webBuilder.UseStartup<Startup>();
         webBuilder.UseUrls("http://localhost:7000"/*, "https://localhost:7001"*/);
     }).ConfigureLogging(logging =>
       {
           logging.ClearProviders();
           logging.AddConsole();
           logging.SetMinimumLevel(LogLevel.Trace);

       })
    .ConfigureServices((hostContext, services) =>
    {
        //services.AddHostedService<SuHatIzlemeWorker>();
        //services.AddHostedService<SuHatIzlemeNodeRedWorker>();
        //services.AddHostedService<SuHatIzlemeNodeRedWorkerTum>();
        services.AddHostedService<SuHatIzlemeNodeRedWorkerTumUpsert>();

        //var suHatScadaConnectionString = hostContext.Configuration.GetConnectionString("SuHatScadaSqlServer");
        //if (string.IsNullOrEmpty(suHatScadaConnectionString))
        //{
        //    throw new InvalidOperationException("SuHatScada Connection string is not configured.");
        //}

        var connectionString = hostContext.Configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string is not configured.");
        }
        services.AddDbContext<SuHatScadaDbContext>((provider, optionsBuilder) =>
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

        services.AddScoped<SqlConnection>(provider => new SqlConnection(connectionString));
        services.AddSingleton<AlarmCacheService>();
        services.AddSingleton<EmailSender>();
        services.AddMemoryCache();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();