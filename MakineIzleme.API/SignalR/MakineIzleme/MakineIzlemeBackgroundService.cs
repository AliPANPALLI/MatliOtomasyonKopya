using MakineIzleme.Application.Interfaces;
using MakineIzleme.Domain.Models.ReportModels;
using Microsoft.AspNetCore.SignalR;

namespace MakineIzleme.API.SignalR.MakineIzleme
{
    public class MakineIzlemeBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<MakineIzlemeHub> _hubContext;
        private readonly IBolumSubscriptionStore _store;
        public MakineIzlemeBackgroundService(IServiceProvider serviceProvider, IHubContext<MakineIzlemeHub> hubContext, IBolumSubscriptionStore store)
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
                    var activeBolums = _store.GetActiveBolums();
                    if (activeBolums.Count > 0)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IMakineIzlemeAppService>();

                        foreach (var bolumId in activeBolums)
                        {
                            var data = await svc.BuildDashboardData(bolumId, stoppingToken);
                            await _hubContext.Clients.Group($"bolum:{bolumId}").SendAsync("ReceiveMachineData", data, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MakineIzlemeBackgroundService error: {ex}");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}