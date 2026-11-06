using EnerjiIzlemeAlarmWorker;
using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Infrastructure;
using EnergyMonitoring.Infrastructure.Mongo;
using EnergyMonitoring.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
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
        services.AddHostedService<EnerjiAlarmWorker>();

        // MongoDbSettings'i yapýlandýr - ÖNCE tanýmla
        var mongoDbSettings = hostContext.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
        services.AddSingleton(mongoDbSettings);

        // IOptions<MongoDbSettings> olarak da kaydet (EnerjiIzlemeMongoDbContext bunu kullanýyor)
        services.Configure<MongoDbSettings>(hostContext.Configuration.GetSection("MongoDbSettings"));

        // MongoDB client
        services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoDbSettings.ConnectionString));

        // MongoDbContext - IOptions kullanýyor
        services.AddScoped<EnerjiIzlemeMongoDbContext>();

        services.AddScoped<ILogsRepository, LogsRepository>();
        services.AddScoped<ITreeRepository, TreeRepository>();

        var enerjiIzlemeConnectionString = hostContext.Configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrEmpty(enerjiIzlemeConnectionString))
        {
            throw new InvalidOperationException("Enerjiizleme Connection string is not configured.");
        }
        services.AddDbContext<EnerjiIzlemeDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(enerjiIzlemeConnectionString);
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
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();