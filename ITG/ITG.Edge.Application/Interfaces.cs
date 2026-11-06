using ITG.Edge.Contracts;

namespace ITG.Edge.Application;

public sealed class ItgOptions
{
    public string PortName { get; set; } = "COM6";
    public bool AutoDiscover { get; set; } = true;
    public string[] AllowedPorts { get; set; } = ["COM6"];
    public int DiscoveryRetrySeconds { get; set; } = 30;
    public int BaudRate { get; set; } = 9600;
    public int[] Controllers { get; set; } = [101];
    public int ScanIntervalSeconds { get; set; } = 180;
    public bool ReadEeprom { get; set; } = true;
    public string SourceTimeZone { get; set; } = "Turkey Standard Time";
}
public sealed class LocationOptions { public string LocationId { get; set; } = "YEM-01"; }
public sealed class OutboxOptions { public string DatabasePath { get; set; } = @"C:\ITG\Data\itg_edge.db"; }
public sealed class RabbitMqOptions
{
    public string CaCertificatePath { get; set; } =
    @"C:\ITG\Certificates\ca.crt";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 5671;
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "itg.telemetry";
    public string RoutingKeyPrefix { get; set; } = "itg.temperature";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string ServerName { get; set; } = "";
}
public interface IItgReader { Task<IReadOnlyList<SensorReading>> ScanAsync(CancellationToken cancellationToken); }
public interface IOutbox
{
    Task<int> CleanupPublishedAsync(
    int retentionDays,
    CancellationToken ct);
    Task InitializeAsync(CancellationToken ct);
    Task SaveAsync(IReadOnlyCollection<TemperatureBatchMessage> messages, CancellationToken ct);
    Task<PendingMessage?> ClaimAsync(CancellationToken ct);
    Task MarkPublishedAsync(string id, CancellationToken ct);
    Task RescheduleAsync(string id, int retryCount, string error, CancellationToken ct);
    Task MarkFailedAsync(string id, string error, CancellationToken ct);
}
public sealed record PendingMessage(string Id, string Payload, int RetryCount);
/// <summary>
/// Kalici hata: mesaj tekrar denense de gonderilemez (bozuk JSON, lokasyon uyusmazligi).
/// Ag/broker hatalari bu tipte firlatilmaz; onlar her zaman yeniden denenir.
/// </summary>
public sealed class InvalidOutboxMessageException(string message, Exception? inner = null)
    : Exception(message, inner);
public interface IMessagePublisher { Task PublishAsync(PendingMessage message, string locationId, CancellationToken ct); }
