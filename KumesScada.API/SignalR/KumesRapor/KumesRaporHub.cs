// Allow anonymous access for this hub - no authorization required
using Microsoft.AspNetCore.SignalR;

namespace KumesScada.API.SignalR.KumesRapor
{
    public class KumesRaporHub : Hub
    {
        public async Task JoinKumesTabloRaporGroup(int lokasyonId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"KumesTabloRapor_lokasyon_{lokasyonId}");
            KumesRaporGroupTracker.IncKumesTablo(lokasyonId);
        }

        public async Task LeaveKumesTabloRaporGroup(int lokasyonId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"KumesTabloRapor_lokasyon_{lokasyonId}");
            KumesRaporGroupTracker.DecKumesTablo(lokasyonId);
        }

        public async Task JoinTesisGenelRaporGroup(int lokasyonId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"TesisGenelRapor_lokasyon_{lokasyonId}");
            KumesRaporGroupTracker.IncTesisGenel(lokasyonId);
        }

        public async Task LeaveTesisGenelRaporGroup(int lokasyonId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"TesisGenelRapor_lokasyon_{lokasyonId}");
            KumesRaporGroupTracker.DecTesisGenel(lokasyonId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // İsteğe bağlı: Eğer client'lar Leave çağırmıyorsa,
            // bağlantı bazlı bir index tutup burada düşmek daha doğru.
            await base.OnDisconnectedAsync(exception);
        }
    }
}