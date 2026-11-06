
using ITG.Edge.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITG.Edge.Application;

public sealed class MeasurementScanService(
    IItgReader reader,
    IOutbox outbox,
    IOptions<LocationOptions> location,
    ILogger<MeasurementScanService> log)
{
    // (ControllerId, MeasuredAt) -> o zaman damgasiyla outbox'a yazilmis sensor anahtarlari.
    // Cihaz yeni olcum almadiysa ayni olcum her taramada tekrar okunur; bunlar tekrar yazilmaz.
    // Sadece ScanBackgroundService tarafindan sirali cagrilir; kilit gerekmez.
    // Bellekte tutulur: servis yeniden basladiginda en fazla bir kez tekrar gider,
    // tuketici tarafi (SensorId, MeasuredAt) cakismasini zaten yok sayar.
    private readonly Dictionary<(int ControllerId, DateTime MeasuredAt), HashSet<string>> _saved = new();
    private static readonly TimeSpan SavedRetention = TimeSpan.FromDays(1);

    public async Task ExecuteScanAsync(CancellationToken ct)
    {
        IReadOnlyList<SensorReading> readings =
            await reader.ScanAsync(ct);

        string locationId = location.Value.LocationId;

        if (string.IsNullOrWhiteSpace(locationId))
            throw new InvalidOperationException(
                "LocationId bos olamaz.");

        var messages = readings
            .GroupBy(x => (
                x.ControllerId,
                x.LocalMeasuredAt))
            .Select(group => new TemperatureBatchMessage(
                MessageId: Guid.NewGuid(),
                MessageType: "itg.temperature.telemetry",
                LocationId: locationId,
                ControllerId: group.Key.ControllerId,
                MeasuredAt: DateTime.SpecifyKind(
                    group.Key.LocalMeasuredAt,
                    DateTimeKind.Unspecified),
                Temperatures: group
                    .GroupBy(x => x.SensorKey)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Last().Temperature)
            ))
            .ToArray();

        // Yalnizca daha once yazilmamis sensor iceren mesajlar gonderilir. Mesajin kendisi
        // degismez: yeni bir sensor varsa grup eskisi gibi eksiksiz gider.
        var fresh = messages
            .Where(IsNotSavedYet)
            .ToArray();

        await outbox.SaveAsync(fresh, ct);

        Remember(fresh);

        log.LogInformation(
            "ITG scan saved | Location={Location} | Sensors={Sensors} | Messages={Messages} | SkippedDuplicates={Skipped}",
            locationId,
            readings.Count,
            fresh.Length,
            messages.Length - fresh.Length);
    }

    private bool IsNotSavedYet(TemperatureBatchMessage message) =>
        !_saved.TryGetValue((message.ControllerId, message.MeasuredAt), out var keys) ||
        !keys.IsSupersetOf(message.Temperatures.Keys);

    private void Remember(IEnumerable<TemperatureBatchMessage> saved)
    {
        foreach (var message in saved)
        {
            var key = (message.ControllerId, message.MeasuredAt);
            if (!_saved.TryGetValue(key, out var keys))
                _saved[key] = keys = new HashSet<string>(StringComparer.Ordinal);
            keys.UnionWith(message.Temperatures.Keys);
        }

        if (_saved.Count == 0)
            return;

        // Cihaz saatine gore 1 gunden eski kayitlar unutulur; bellek sinirsiz buyumez.
        DateTime cutoff = _saved.Keys.Max(x => x.MeasuredAt) - SavedRetention;
        foreach (var key in _saved.Keys.Where(x => x.MeasuredAt < cutoff).ToList())
            _saved.Remove(key);
    }
}
