using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace ItgTemperatureIngestionWorker;

/// <summary>
/// Kuyruktan gelen mesajlar bir lokasyonun (facility) tum sensorlerini degil,
/// o anki kontrolorun gordugu alt kumeyi tasir (orn. OSB-YEM icin 6 ayri
/// kontrolor, her biri farkli depolari kapsar). Bu servis lokasyon bazinda
/// en son degerleri bellekte birlestirir (merge) ve o lokasyona bagli
/// WebSocket istemcilerine guncel, birlesik "temperatures" haritasini yayinlar.
/// </summary>
public sealed class FacilityBroadcastService
{
    private readonly ILogger<FacilityBroadcastService> _logger;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, double>> _snapshotByFacility = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, ClientConnection>> _clientsByFacility = new();

    public FacilityBroadcastService(ILogger<FacilityBroadcastService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Yeni mesajdaki sensor degerlerini lokasyonun birlesik anlik goruntusune (snapshot)
    /// isler, ardindan o lokasyona bagli tum WebSocket istemcilerine guncel haritayi yayinlar.
    /// </summary>
    public async Task UpdateAndBroadcastAsync(
        string locationId,
        DateTime measuredAt,
        IReadOnlyDictionary<string, double> temperatures,
        CancellationToken cancellationToken)
    {
        var snapshot = _snapshotByFacility.GetOrAdd(locationId, _ => new ConcurrentDictionary<string, double>());

        foreach (var (sensorKey, value) in temperatures)
        {
            snapshot[sensorKey] = value;
        }

        if (!_clientsByFacility.TryGetValue(locationId, out var clients) || clients.IsEmpty)
            return;

        var payload = BuildPayloadBytes(locationId, measuredAt, snapshot);

        foreach (var connection in clients.Values)
        {
            await connection.SendAsync(payload, cancellationToken);
        }
    }

    /// <summary>
    /// Bir WebSocket baglantisini bir lokasyona kaydeder, mevcut birlesik snapshot'i
    /// hemen gonderir ve baglanti kapanana kadar bekler. Cagiran taraf (endpoint)
    /// bu Task tamamlandiginda soketi kapatabilir.
    /// </summary>
    public async Task HandleClientAsync(string locationId, WebSocket socket, CancellationToken cancellationToken)
    {
        var connection = new ClientConnection(socket);
        var clients = _clientsByFacility.GetOrAdd(locationId, _ => new ConcurrentDictionary<Guid, ClientConnection>());
        var connectionId = Guid.NewGuid();
        clients[connectionId] = connection;

        _logger.LogInformation("WebSocket istemcisi baglandi. LocationId: {LocationId}", locationId);

        try
        {
            if (_snapshotByFacility.TryGetValue(locationId, out var snapshot))
            {
                var initialPayload = BuildPayloadBytes(locationId, DateTime.UtcNow, snapshot);
                await connection.SendAsync(initialPayload, cancellationToken);
            }

            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Kapatildi", cancellationToken);
                    break;
                }

                // Bu soket sadece yayin icin kullaniliyor; istemciden gelen veri yok sayilir.
            }
        }
        catch (OperationCanceledException)
        {
            // Servis kapatiliyor; normal akis.
        }
        catch (WebSocketException)
        {
            // Istemci beklenmedik sekilde koptu; normal akis.
        }
        finally
        {
            clients.TryRemove(connectionId, out _);
            _logger.LogInformation("WebSocket istemcisi ayrildi. LocationId: {LocationId}", locationId);
        }
    }

    private static byte[] BuildPayloadBytes(string locationId, DateTime updatedAt, IDictionary<string, double> snapshot)
    {
        var payload = new
        {
            locationId,
            updatedAt,
            temperatures = snapshot.ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        return JsonSerializer.SerializeToUtf8Bytes(payload);
    }

    private sealed class ClientConnection
    {
        private readonly WebSocket _socket;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public ClientConnection(WebSocket socket)
        {
            _socket = socket;
        }

        public async Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (_socket.State != WebSocketState.Open)
                return;

            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (_socket.State == WebSocketState.Open)
                {
                    await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
                }
            }
            catch (WebSocketException)
            {
                // Baglanti kopmus olabilir; temizlik HandleClientAsync'in finally'sinde yapilir.
            }
            finally
            {
                _sendLock.Release();
            }
        }
    }
}
