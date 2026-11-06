using KumesScada.API.SignalR.KumesScada;
using KumesScada.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class VeriGuncellemeBackgroundService : BackgroundService
{
    private readonly IHubContext<SignalRHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ConcurrentDictionary<int, (int Interval, int UserCount)> _aktifKumesler = new();
    private readonly ConcurrentDictionary<int, (int Interval, int UserCount)> _aktifKonveyorler = new(); // konveyorId, interval
    private readonly Dictionary<int, DateTime> _kumesLastSent = new();
    private readonly Dictionary<int, DateTime> _konveyorLastSent = new();

    public VeriGuncellemeBackgroundService(IHubContext<SignalRHub> hubContext, IServiceScopeFactory scopeFactory)
    {
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public void AddOrUpdateKumes(int kumesId, int intervalInSeconds)
    {
        _aktifKumesler.AddOrUpdate(kumesId,
               (intervalInSeconds, 1),
               (key, oldValue) => (intervalInSeconds, oldValue.UserCount + 1));
    }

    public void RemoveKumes(int kumesId)
    {
        if (_aktifKumesler.TryGetValue(kumesId, out var value))
        {
            if (value.UserCount <= 1)
            {
                _aktifKumesler.TryRemove(kumesId, out _);
                _kumesLastSent.Remove(kumesId);
            }
            else
            {
                _aktifKumesler[kumesId] = (value.Interval, value.UserCount - 1);
            }
        }
    }

    public void AddOrUpdateKonveyor(int konveyorId, int intervalInSeconds)
    {
        _aktifKonveyorler.AddOrUpdate(konveyorId,
              (intervalInSeconds, 1),
              (key, oldValue) => (intervalInSeconds, oldValue.UserCount + 1));
    }

    public void RemoveKonveyor(int konveyorId)
    {
        if (_aktifKonveyorler.TryGetValue(konveyorId, out var value))
        {
            if (value.UserCount <= 1)
            {
                _aktifKonveyorler.TryRemove(konveyorId, out _);
                _konveyorLastSent.Remove(konveyorId);
            }
            else
            {
                _aktifKonveyorler[konveyorId] = (value.Interval, value.UserCount - 1);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cizimAppService = scope.ServiceProvider.GetRequiredService<ICizimAppService>();

                // KÜMESLER
                foreach (var (kumesId, data) in _aktifKumesler)
                {
                    if (!_kumesLastSent.TryGetValue(kumesId, out var lastSent) || (DateTime.UtcNow - lastSent).TotalSeconds >= data.Interval)
                    {
                        var elements = await cizimAppService.GetCizimJsonByKumesId(kumesId);
                        await _hubContext.Clients.Group($"kumes_{kumesId}").SendAsync($"receiveCizimData_{kumesId}", elements);
                        _kumesLastSent[kumesId] = DateTime.UtcNow;
                    }
                }

                // KONVEYORLER
                foreach (var (konveyorId, data) in _aktifKonveyorler)
                {
                    if (!_konveyorLastSent.TryGetValue(konveyorId, out var lastSent) || (DateTime.UtcNow - lastSent).TotalSeconds >= data.Interval)
                    {
                        var elements = await cizimAppService.GetCizimJsonById(konveyorId);
                        await _hubContext.Clients.Group($"konveyor_{konveyorId}").SendAsync($"receiveCizimData_{konveyorId}", elements);
                        _konveyorLastSent[konveyorId] = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BackgroundService HATA]: {ex.Message}");
            }

            await Task.Delay(1000, stoppingToken); // 1 saniyelik ana döngü
        }
    }
}