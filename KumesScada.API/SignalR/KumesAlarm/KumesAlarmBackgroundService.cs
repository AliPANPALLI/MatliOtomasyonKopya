using KumesScada.API.SignalR.KumesAlarm;
using KumesScada.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

public class KumesAlarmBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<KumesAlarmHub> _hubContext;

    public KumesAlarmBackgroundService(IServiceProvider serviceProvider, IHubContext<KumesAlarmHub> hubContext)
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
                    var aktifPairs = KumesAlarmGroupTracker.KumesAlarmUserCounts.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList(); // (kumesId, userId)
                    foreach (var (kumesId, userId) in aktifPairs)
                    {
                        var aktifAlarmlar = await raporService.GetAktifAlarmLogForKumes(kumesId);
                        var pasifAlarmlar = await raporService.GetPasifAlarmLogForKumes(kumesId);
                        aktifAlarmlar.AddRange(pasifAlarmlar);
                        await _hubContext.Clients.Group($"KumesAlarm_kumes_{kumesId}_user_{userId}").SendAsync("ReceiveKumesAlarm", aktifAlarmlar, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KumesAlarmBackgroundService error: {ex}");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}