using MakineIzleme.Application.Interfaces;
using MakineIzleme.Domain.Models.ReportModels;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace MakineIzleme.API.SignalR.MakineIzleme
{
    public class MakineIzlemeHub : Hub
    {

        private readonly IMakineIzlemeAppService _svc;
        private readonly IBolumSubscriptionStore _store;

        public MakineIzlemeHub(IMakineIzlemeAppService svc, IBolumSubscriptionStore store)
        {
            _svc = svc;
            _store = store;
        }

        // (Opsiyonel) Client modal açınca çağırır
        public async Task<List<MachineLogRowDto>> GetMachineLogs(int machineId, int take = 100)
        {
            return await _svc.GetMachineLogs(machineId, take);
        }

        public async Task JoinBolum(int bolumId)
        {
            _store.Touch(bolumId);
            // aynı bağlantı başka bölümdeyse önce temizle (opsiyonel ama önerilir)
            var oldBolum = Context.Items.TryGetValue("bolumId", out var v) ? v as int? : null;
            if (oldBolum.HasValue)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"bolum:{oldBolum.Value}");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"bolum:{bolumId}");
            Context.Items["bolumId"] = bolumId;
        }

        // (Opsiyonel) Site/lokasyon bazlı grup istersen:
        //public async Task Subscribe(string groupKey)
        //{
        //    await Groups.AddToGroupAsync(Context.ConnectionId, groupKey);
        //}

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