using ITG.Edge.Application;
using ITG.Edge.Infrastructure.RabbitMq;
using ITG.Edge.Infrastructure.Serial;
using ITG.Edge.Infrastructure.Sqlite;
using ITG.Edge.Worker;
using Serilog;

Directory.CreateDirectory(@"C:\ITG\Logs");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(@"C:\ITG\Logs\itg-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();
try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options => options.ServiceName = "Matli ITG Edge Worker");
    builder.Logging.ClearProviders();
    builder.Services.AddSerilog();
    builder.Services.Configure<LocationOptions>(builder.Configuration.GetSection("Location"));
    builder.Services.Configure<ItgOptions>(builder.Configuration.GetSection("ITG"));
    builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));
    builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.AddSingleton<ItgDeviceDiscoveryService>();
    builder.Services.AddSingleton<IItgReader, ItgSerialReader>();
    builder.Services.AddSingleton<IOutbox, SqliteOutbox>();
    builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
    builder.Services.AddSingleton<MeasurementScanService>();
    builder.Services.AddHostedService<ScanBackgroundService>();
    builder.Services.AddHostedService<PublishBackgroundService>();
    var host = builder.Build();
    // Initialize the local outbox before scan and publisher start (startup order independent).
    await host.Services.GetRequiredService<IOutbox>().InitializeAsync(CancellationToken.None);
    await host.RunAsync();
}
catch (Exception ex)
{
    // Baslangic/host hatalari da dosyaya yazilsin; aksi halde servis iz birakmadan kapanir.
    Log.Fatal(ex, "ITG Edge Worker terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
