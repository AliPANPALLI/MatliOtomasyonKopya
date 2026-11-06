using PoultryHouseScada.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using RahmiyeTesisWorker;
using RahmiyeTesisWorker.Core;
using Serilog;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;
using System.Data.SqlClient;

IHost host = Host.CreateDefaultBuilder(args)
     .ConfigureWebHostDefaults(webBuilder =>
     {
         webBuilder.UseStartup<Startup>();
         webBuilder.UseUrls("http://localhost:5000"/*, "https://localhost:5001"*/);
     })
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
        //services.AddHostedService<RahmiyeWorker>();
        services.AddHostedService<RahmiyeNodeRedWorker>();

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
        services.AddSingleton<AlarmCacheService>();
        services.AddSingleton<EmailSender>();
        var sharedConnectionString = hostContext.Configuration.GetConnectionString("SharedSqlServer");
        if (string.IsNullOrEmpty(sharedConnectionString))
        {
            throw new InvalidOperationException("Shared Connection string is not configured.");
        }
        services.AddDbContext<SharedDbContext>((provider, optionsBuilder) =>
        {
            optionsBuilder.UseSqlServer(sharedConnectionString);
        }, ServiceLifetime.Scoped);
        services.AddMemoryCache();
    })
    .UseWindowsService() // Windows Service desteðini etkinleþtiriyoruz
    .Build();

await host.RunAsync();

//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace RahmiyeTesisWorker
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            // Worker servisini baþlatan ve Windows servisi olarak çalýþacak þekilde ayarlayan yapý
//            CreateHostBuilder(args).Build().Run();
//        }

//        public static IHostBuilder CreateHostBuilder(string[] args) =>
//            Host.CreateDefaultBuilder(args)
//                .ConfigureServices((context, services) =>
//                {
//                    // Worker servisini burada kaydediyoruz
//                    services.AddHostedService<RahmiyeWorker>();
//                })
//                .ConfigureLogging((context, logging) =>
//                {
//                    logging.ClearProviders(); // Varsayýlan saðlayýcýlarý temizle
//                    logging.AddConsole(); // Konsola log yazmak için ekliyoruz
//                    logging.SetMinimumLevel(LogLevel.Trace); // Log seviyesini en düþük seviyeye ayarla
//                })
//                .UseWindowsService(); // Windows servisi olarak çalýþacak þekilde ayarlýyoruz
//    }
//}