using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace SuHatScada.API
{
    public class SignalRHubExpert2 : Hub
    {
        private readonly VeriGuncellemeExpertBackgroundService2 _periodicService;

        // ConnectionId -> (lokasyonId, userId)
        private static readonly ConcurrentDictionary<string, (int lokasyonId, string userId)> _connectionInfo = new();

        public SignalRHubExpert2(VeriGuncellemeExpertBackgroundService2 periodicService)
        {
            _periodicService = periodicService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task JoinLocationGroup(string lokasyonIdStr, int intervalInSeconds)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(lokasyonIdStr, out int lokasyonId) || string.IsNullOrWhiteSpace(userId))
                return;

            string groupName = $"lokasyon_{lokasyonId}_user_{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _connectionInfo[Context.ConnectionId] = (lokasyonId, userId);
            _periodicService.AddOrUpdateLokasyonUser(lokasyonId, userId, intervalInSeconds);
        }

        public async Task LeaveLocationGroup(string lokasyonIdStr)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(lokasyonIdStr, out int lokasyonId) || string.IsNullOrWhiteSpace(userId))
                return;

            string groupName = $"lokasyon_{lokasyonId}_user_{userId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _connectionInfo.TryRemove(Context.ConnectionId, out _);
            _periodicService.RemoveLokasyonUser(lokasyonId, userId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectionInfo.TryRemove(Context.ConnectionId, out var info))
            {
                string groupName = $"lokasyon_{info.lokasyonId}_user_{info.userId}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                _periodicService.RemoveLokasyonUser(info.lokasyonId, info.userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

}