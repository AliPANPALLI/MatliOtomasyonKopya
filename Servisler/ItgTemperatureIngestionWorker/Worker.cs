using System.Text;
using System.Text.Json;
using ItgTemperatureIngestionWorker.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ItgTemperatureIngestionWorker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly TemperatureIngestRepository _repository;
    private readonly SensorCache _sensorCache;
    private readonly FacilityBroadcastService _broadcastService;
    private readonly RabbitMqOptions _rabbitOptions;

    private IConnection? _connection;
    private IChannel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Worker(
        ILogger<Worker> logger,
        TemperatureIngestRepository repository,
        SensorCache sensorCache,
        FacilityBroadcastService broadcastService,
        IOptions<RabbitMqOptions> rabbitOptions)
    {
        _logger = logger;
        _repository = repository;
        _sensorCache = sensorCache;
        _broadcastService = broadcastService;
        _rabbitOptions = rabbitOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _sensorCache.RefreshAsync(stoppingToken, force: true);

        var factory = new ConnectionFactory
        {
            HostName = _rabbitOptions.HostName,
            Port = _rabbitOptions.Port,
            VirtualHost = _rabbitOptions.VirtualHost,
            UserName = _rabbitOptions.UserName,
            Password = _rabbitOptions.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Exchange/kuyruk/binding RabbitMQ tarafında (itg vhost'u, quorum kuyruk,
        // itg-ingestion-dlq dead-letter exchange'i ile) zaten tanımlı. Burada tekrar
        // declare edilmiyor; farklı argümanlarla declare etmeye çalışmak mevcut quorum
        // kuyrukla PRECONDITION_FAILED hatası verir. Sadece var olan kuyruğa abone oluyoruz.
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 50,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                await HandleMessageAsync(ea.Body.ToArray(), stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "RabbitMQ mesajı deserialize edilemedi, mesaj atlanıyor.");

                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ mesajı işlenirken hata oluştu, mesaj yeniden kuyruğa alınıyor.");

                if (_channel is { IsOpen: true })
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _rabbitOptions.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "RabbitMQ consumer başladı. VirtualHost: {VirtualHost}, Queue: {Queue}",
            _rabbitOptions.VirtualHost,
            _rabbitOptions.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(byte[] body, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(body);

        var message = JsonSerializer.Deserialize<TemperatureTelemetryMessage>(json, JsonOptions);

        if (message is null || message.Temperatures.Count == 0)
        {
            _logger.LogWarning("Mesaj boş veya sıcaklık verisi içermiyor. Mesaj atlanıyor.");
            return;
        }

        var controllerCode = message.ControllerId.ToString();

        var readings = new List<SensorReading>(message.Temperatures.Count);
        var missingKeys = new List<string>();

        foreach (var (sensorKey, temperature) in message.Temperatures)
        {
            if (TryResolveSensorId(message.LocationId, controllerCode, sensorKey, out var sensorId))
            {
                readings.Add(new SensorReading(sensorId, message.MeasuredAt, temperature));
            }
            else
            {
                missingKeys.Add(sensorKey);
            }
        }

        if (missingKeys.Count > 0)
        {
            // Eşleme tablosunda yeni sensör eklenmiş olabilir, bir kere taze veriyle tekrar dene.
            await _sensorCache.RefreshAsync(cancellationToken, force: true);

            var stillMissing = new List<string>();
            foreach (var sensorKey in missingKeys)
            {
                if (TryResolveSensorId(message.LocationId, controllerCode, sensorKey, out var sensorId))
                {
                    readings.Add(new SensorReading(sensorId, message.MeasuredAt, message.Temperatures[sensorKey]));
                }
                else
                {
                    stillMissing.Add(sensorKey);
                }
            }

            if (stillMissing.Count > 0)
            {
                _logger.LogWarning(
                    "Eşlenemeyen sensör anahtarları atlandı. LocationId: {LocationId}, ControllerId: {ControllerId}, Anahtarlar: {Keys}, MessageId: {MessageId}",
                    message.LocationId,
                    message.ControllerId,
                    string.Join(", ", stillMissing),
                    message.MessageId);
            }
        }

        // GECICI: WebSocket yayinini sahte/test verisiyle denerken DB'ye yazmayi
        // devre disi birakmak icin yorum satirina alindi. Test bitince geri acilacak.
        await _repository.InsertReadingsAndUpsertLatestAsync(readings, cancellationToken);

        await _broadcastService.UpdateAndBroadcastAsync(
            message.LocationId,
            message.MeasuredAt,
            message.Temperatures,
            cancellationToken);

        _logger.LogInformation(
            "LocationId: {LocationId}, ControllerId: {ControllerId}: {Count} sensör kaydedildi. MessageId: {MessageId}",
            message.LocationId,
            message.ControllerId,
            readings.Count,
            message.MessageId);
    }

    private bool TryResolveSensorId(string locationId, string controllerCode, string sensorKey, out Guid sensorId)
        => _sensorCache.TryGetSensorId(locationId, controllerCode, sensorKey, out sensorId);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
