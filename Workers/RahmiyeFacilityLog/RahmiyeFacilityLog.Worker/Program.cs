using PoultryHouseScada.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RahmiyeFacilityLog.Worker;
using RahmiyeFacilityLog.Worker.Core;
using RahmiyeFacilityLog.Worker.Models;
using SharedApp.Infrastructure;

IHost host = Host.CreateDefaultBuilder(args)
     //.ConfigureWebHostDefaults(webBuilder =>
     //{
     //    webBuilder.UseStartup<Startup>();
     //    //webBuilder.UseUrls("http://localhost:5000"/*, "https://localhost:5001"*/);
     //})
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<RahmiyeTesisLogNodeRedWorker>();

        var connectionString = hostContext.Configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string is not configured.");
        }

        services.AddDbContext<KumesScadaDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        services.AddScoped<SqlConnection>(provider => new SqlConnection(connectionString));
        var sharedConnectionString = hostContext.Configuration.GetConnectionString("SharedSqlServer");
        if (string.IsNullOrEmpty(sharedConnectionString))
        {
            throw new InvalidOperationException("Shared Connection string is not configured.");
        }
        services.AddDbContext<SharedDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(sharedConnectionString);
        }, ServiceLifetime.Scoped);
        services.AddSingleton<CacheService>();
        services.AddMemoryCache();

        services.Configure<SapSettings>(hostContext.Configuration.GetSection("Sap"));
        services.AddHttpClient<SapKantarService>();
    })
    .UseWindowsService() // Windows Service deste�ini etkinle�tiriyoruz
    .Build();

await host.RunAsync();