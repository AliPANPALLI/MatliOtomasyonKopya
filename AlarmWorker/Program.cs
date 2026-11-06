using AlarmWorker;
using KumesScada.Infrastructure;
using SharedApp.Infrastructure;
using SuHatScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Infrastructure.Sender;
using EnerjiIzleme.Infrastructure;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Trace);
    })
    .ConfigureServices((hostContext, services) =>
    {
        // Connection string'lerini alýyoruz
        var kumesScadaConnectionString = hostContext.Configuration.GetConnectionString("KumesScadaSqlServer");
        if (string.IsNullOrEmpty(kumesScadaConnectionString))
        {
            throw new InvalidOperationException("KumesScada Connection string is not configured.");
        }

        var suHatScadaConnectionString = hostContext.Configuration.GetConnectionString("SuHatScadaSqlServer");
        if (string.IsNullOrEmpty(suHatScadaConnectionString))
        {
            throw new InvalidOperationException("SuHatScada Connection string is not configured.");
        }

        var enerjiIzlemeConnectionString = hostContext.Configuration.GetConnectionString("EnerjiIzlemeSqlServer");
        if (string.IsNullOrEmpty(enerjiIzlemeConnectionString))
        {
            throw new InvalidOperationException("EnerjiIzleme Connection string is not configured.");
        }

        var sharedConnectionString = hostContext.Configuration.GetConnectionString("SharedSqlServer");
        if (string.IsNullOrEmpty(sharedConnectionString))
        {
            throw new InvalidOperationException("Shared Connection string is not configured.");
        }

        // DbContext'leri scoped olarak ekliyoruz
        services.AddDbContext<KumesScadaDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(kumesScadaConnectionString);
        }, ServiceLifetime.Scoped);

        services.AddDbContext<SuHatScadaDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(suHatScadaConnectionString);
        }, ServiceLifetime.Scoped);

        services.AddDbContext<EnerjiIzlemeDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(enerjiIzlemeConnectionString);
        }, ServiceLifetime.Scoped);

        services.AddDbContext<SharedDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(sharedConnectionString);
        }, ServiceLifetime.Scoped);

        services.AddSingleton<EmailSender>();
        // Worker servisini ekliyoruz
        //services.AddHostedService<AlarmBilgilendirmeServis>();
        services.AddHostedService<AlarmBilgilendirmeService>();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();