using EnerjiIzleme.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EnerjiIzleme.API.SignalR
{
    public class KumesEnerjiIzlemeBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<KumesEnerjiIzlemeHub> _hubContext;
        private readonly IKumesEnerjiSubscriptionStore _store;
        public KumesEnerjiIzlemeBackgroundService(IServiceProvider serviceProvider, IHubContext<KumesEnerjiIzlemeHub> hubContext, IKumesEnerjiSubscriptionStore store)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _store = store;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var activeKumesTipler = _store.GetActiveBolums();
                    if (activeKumesTipler.Count > 0)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<ITreeAppService>();

                        foreach (var kumesTip in activeKumesTipler)
                        {
                            var data = await svc.BuildEnergyPayloadForFarms(kumesTip, stoppingToken);
                            await _hubContext.Clients.Group($"kumesTip:{kumesTip}").SendAsync("EnergyData", data, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"KumesEnerjiIzlemeBackgroundService error: {ex}");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
