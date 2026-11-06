using System.Text.RegularExpressions;
using ItgTemperatureIngestion.Worker;
using Microsoft.Extensions.Hosting.WindowsServices;
using ItgTemperatureIngestion.Infrastructure.Caching;
using ItgTemperatureIngestion.Infrastructure.Persistence;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentPlaceholders();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ItgTemperatureIngestionWorker";
});

// "Postgres" gibi genel bir anahtar adi, makinede baska bir worker icin tanimlanmis
// ConnectionStrings__Postgres sistem/ortam degiskeniyle carpisip appsettings.json'daki
// degeri sessizce ezebiliyordu. Bu yuzden ITG'ye ozgu, benzersiz bir anahtar kullaniyoruz.
var connectionString =
    builder.Configuration.GetConnectionString("ItgPostgres")
    ?? throw new InvalidOperationException("ItgPostgres connection string bulunamadı.");

// Hangi appsettings.json'ın ve hangi Host/Port/Database degerinin fiilen
// kullanildigini gormek icin, sifre maskelenerek konsola yaziliyor.
var maskedConnectionString = Regex.Replace(connectionString, "(Password=)[^;]*", "$1***", RegexOptions.IgnoreCase);
Console.WriteLine($"[STARTUP] Kullanilan appsettings.json: {Path.Combine(AppContext.BaseDirectory, "appsettings.json")}");
Console.WriteLine($"[STARTUP] Postgres baglanti bilgisi: {maskedConnectionString}");

var webSocketPort = builder.Configuration.GetValue<int>("WebSocketPort", 6060);
builder.WebHost.UseUrls($"http://0.0.0.0:{webSocketPort}");
Console.WriteLine($"[STARTUP] WebSocket portu: {webSocketPort}");

builder.Services.AddSingleton(new PostgresConnectionFactory(connectionString));
builder.Services.AddSingleton<SensorCache>();
builder.Services.AddSingleton<TemperatureIngestRepository>();
builder.Services.AddSingleton<FacilityBroadcastService>();

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.UseWebSockets();

// Ornek: ws://<sunucu>:6060/ws/OSB-YEM  ve  ws://<sunucu>:6060/ws/MERKEZ-YEM
// locationId, mesajlardaki "locationId" degeriyle birebir eslesmeli (buyuk/kucuk harf duyarli degil).
app.Map("/ws/siloscada/{locationId}", async (HttpContext context, string locationId, FacilityBroadcastService broadcastService) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    // Frontend token'ı alt protokol olarak gönderir: new WebSocket(url, ["Bearer", jwt]).
    // Tarayıcı, istediği alt protokollerden biri cevapta geri gelmezse bağlantıyı reddeder;
    // Ocelot da burada seçilen alt protokolü tarayıcıya aynen iletir. Token Gateway'de doğrulanır.
    var subProtocol = context.WebSockets.WebSocketRequestedProtocols.Contains("Bearer") ? "Bearer" : null;
    using var socket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);
    await broadcastService.HandleClientAsync(locationId.ToUpperInvariant(), socket, context.RequestAborted);
});

var isWindowsService = WindowsServiceHelpers.IsWindowsService();

app.Run();

// Gercek Windows Service olarak calisirken konsol/klavye yok; sadece konsoldan
// elle calistirildiginda pencerenin aninda kapanmasini onleyip hatayi okunabilir
// kilmak icin bekletiyoruz.
if (!isWindowsService)
{
    Console.WriteLine();
    Console.WriteLine("Servis durdu. Kapatmak icin bir tusa basin...");
    Console.ReadKey();
}
