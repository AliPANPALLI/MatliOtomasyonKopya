using Authentication.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SapEmployeeSync.Worker;
using SapEmployeeSync.Application.Models;
using SapEmployeeSync.Infrastructure.Sap;
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
        // Not: AuthDbContext'in IConfiguration alan constructor'ı OnConfiguring icinde
        // sabit olarak "SqlServer" key'ini okuyor (Authentication.API ile ayni davranis) -
        // asagidaki lambda EF Core'un ActivatorUtilities secimi nedeniyle devreye girmeyebilir,
        // o yuzden appsettings'te de "SqlServer" key'i kullaniliyor.
        var authConnectionString = hostContext.Configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrEmpty(authConnectionString))
        {
            throw new InvalidOperationException("SqlServer connection string is not configured.");
        }

        services.AddDbContext<AuthDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(authConnectionString);
        }, ServiceLifetime.Scoped);

        services.Configure<SapSettings>(hostContext.Configuration.GetSection("Sap"));

        services.AddHttpClient<SapPaMatFm002Service>();
        services.AddHttpClient<SapPerlist03Service>();

        services.AddHostedService<SapPersonelSyncWorker>();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
