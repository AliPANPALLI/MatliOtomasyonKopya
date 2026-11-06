using EnerjiIzleme.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EnerjiIzleme.API.SignalR
{
    public class KumesEnerjiIzlemeHub : Hub
    {

        private readonly ITreeAppService _svc;
        private readonly IKumesEnerjiSubscriptionStore _store;

        public KumesEnerjiIzlemeHub(ITreeAppService svc, IKumesEnerjiSubscriptionStore store)
        {
            _svc = svc;
            _store = store;
        }

        public async Task JoinKumesTip(int kumesTip)
        {
            _store.Touch(kumesTip);
            // aynı bağlantı başka bölümdeyse önce temizle (opsiyonel ama önerilir)
            var oldBolum = Context.Items.TryGetValue("kumesTip", out var v) ? v as int? : null;
            if (oldBolum.HasValue)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kumesTip:{oldBolum.Value}");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"kumesTip:{kumesTip}");
            Context.Items["kumesTip"] = kumesTip;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}