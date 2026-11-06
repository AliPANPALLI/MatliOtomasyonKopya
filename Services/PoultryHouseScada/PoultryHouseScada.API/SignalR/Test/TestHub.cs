using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PoultryHouseScada.API.SignalR.Test
{
    public class TestHub : Hub
    {

        public async Task JoinTestGroup(int kumesTipId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Test_kumesTip_{kumesTipId}");
            TestTracker.Join(Context.ConnectionId, kumesTipId);
        }

        public async Task LeaveTestGroup(int kumesTipId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Test_kumesTip_{kumesTipId}");
            TestTracker.Leave(Context.ConnectionId, kumesTipId);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            TestTracker.Disconnect(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}