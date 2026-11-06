using PoultryHouseScada.API.SignalR.KumesRapor;
using PoultryHouseScada.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

public class KumesRaporBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<KumesRaporHub> _hubContext;

    public KumesRaporBackgroundService(IServiceProvider serviceProvider, IHubContext<KumesRaporHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var raporService = scope.ServiceProvider.GetRequiredService<IKumesAppService>();

                    // 1) KumesTablo (lokasyon bazlı, user bağımsız)
                    var aktifLokasyonlar = KumesRaporGroupTracker.KumesTabloLokasyonSayaci.Keys.ToList();
                    foreach (var lokasyonId in aktifLokasyonlar)
                    {
                        var tabloData = await raporService.GetKumeslerOzetRaporByLokasyonIdHub(lokasyonId);
                        await _hubContext.Clients.Group($"KumesTabloRapor_lokasyon_{lokasyonId}").SendAsync("ReceiveKumesRapor", tabloData, cancellationToken: stoppingToken);
                    }

                    // 2) TesisGenel (lokasyon bazlı, user-independent groups)
                    var aktifTesisLokasyonlar = KumesRaporGroupTracker.TesisGenelLokasyonSayaci.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
                    foreach (var lokasyonId in aktifTesisLokasyonlar)
                    {
                        // kullaniciya özel bilgi istemiyorsak userId parametresi olmayan overload kullanılmalı
                        var tesisGenelData = await raporService.GetTesisGenelIzlemeByLokasyonId(lokasyonId,"");

                        await _hubContext.Clients.Group($"TesisGenelRapor_lokasyon_{lokasyonId}").SendAsync("ReceiveTesisGenel", tesisGenelData, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KumesRaporBackgroundService error: {ex}");
            }

            await Task.Delay(3000, stoppingToken);
        }
    }
}