using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PoultryHouseScada.API.SignalR.KumesAlarm
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class KumesAlarmHub : Hub
    {
        public async Task JoinKumesAlarmGroup(int kumesId)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"KumesAlarm_kumes_{kumesId}_user_{userId}");
                KumesAlarmGroupTracker.IncKumesAlarmlar(kumesId, userId);
            }
        }

        public async Task LeaveKumesAlarmGroup(int kumesId)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"KumesAlarm_kumes_{kumesId}_user_{userId}");
                KumesAlarmGroupTracker.DecKumesAlarmlar(kumesId, userId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // İsteğe bağlı: Eğer client'lar Leave çağırmıyorsa,
            // bağlantı bazlı bir index tutup burada düşmek daha doğru.
            await base.OnDisconnectedAsync(exception);
        }
    }
}