using Microsoft.AspNetCore.SignalR;
using WaterLineScada.Application.Interfaces;
using System.Collections.Concurrent;

namespace WaterLineScada.API
{
    public class VeriGuncellemeBackgroundService : BackgroundService
    {
        private readonly IHubContext<SignalRHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ConcurrentDictionary<int, (int Interval, int UserCount)> _aktifLokasyonlar = new();
        private readonly Dictionary<int, DateTime> _lokasyonLastSent = new();

        public VeriGuncellemeBackgroundService(IHubContext<SignalRHub> hubContext, IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        public void AddOrUpdateLokasyon(int lokasyonId, int intervalInSeconds)
        {
            _aktifLokasyonlar.AddOrUpdate(lokasyonId,
                   (intervalInSeconds, 1),
                   (key, oldValue) => (intervalInSeconds, oldValue.UserCount + 1));
        }

        public void RemoveLokasyon(int lokasyonId)
        {
            if (_aktifLokasyonlar.TryGetValue(lokasyonId, out var value))
            {
                if (value.UserCount <= 1)
                {
                    _aktifLokasyonlar.TryRemove(lokasyonId, out _);
                    _lokasyonLastSent.Remove(lokasyonId);
                }
                else
                {
                    _aktifLokasyonlar[lokasyonId] = (value.Interval, value.UserCount - 1);
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

                    foreach (var (lokasyonId, data) in _aktifLokasyonlar)
                    {
                        if (!_lokasyonLastSent.TryGetValue(lokasyonId, out var lastSent) || (DateTime.UtcNow - lastSent).TotalSeconds >= data.Interval)
                        {
                            var elements = await cizimAppService.GetCizimJsonByLokasyonId(lokasyonId);
                            await _hubContext.Clients.Group($"lokasyon_{lokasyonId}").SendAsync($"receiveCizimData_{lokasyonId}", elements);
                            _lokasyonLastSent[lokasyonId] = DateTime.UtcNow;
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

        public IReadOnlyDictionary<int, (int Interval, int UserCount)> GetActiveLokasyonlar()
        {
            // Güvenli bir kopyasını döndürür
            return new Dictionary<int, (int Interval, int UserCount)>(_aktifLokasyonlar);
        }
    }
}