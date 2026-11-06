using VoiceCallDispatch.Worker;
using SharedApp.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Infrastructure.Sender;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configuration => configuration.AddEnvironmentPlaceholders())
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Trace);

        // Log.Logger = new LoggerConfiguration()
        //.WriteTo.Console()
        //.WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
        //.CreateLogger();

        //logging.ClearProviders();
        //logging.AddSerilog();

        // EventLog saðlayýcýsýný manuel olarak devre dýþý býrak
        //logging.AddFilter((provider, category, logLevel) =>
        //{
        //    // EventLog saðlayýcýsýný iptal ediyoruz
        //    return provider != "Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider";
        //});
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<MesajGonderimTakipWorker>();

        var connectionString = hostContext.Configuration.GetConnectionString("SharedSqlServer");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string is not configured.");
        }

        services.AddDbContext<SharedDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        services.AddSingleton<SesliMesajSender>();
        services.AddSingleton<HttpClient>();
    })
    .UseWindowsService()
    .Build();

await host.RunAsync();
