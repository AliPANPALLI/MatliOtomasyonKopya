using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace SuHatScada.API
{
    public class SignalRHubExpert : Hub
    {
        private readonly VeriGuncellemeExpertBackgroundService _periodicService;

        // ConnectionId -> (lokasyonId, userId)
        private static readonly ConcurrentDictionary<string, (int lokasyonId, string userId)> _connectionInfo = new();

        public SignalRHubExpert(VeriGuncellemeExpertBackgroundService periodicService)
        {
            _periodicService = periodicService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            //Console.WriteLine($"Kullanıcı bağlandı: userId={userId}, role={userRole}");

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                //await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{userRole}");
            }

            await base.OnConnectedAsync();
        }

        public async Task JoinLocationGroup(string lokasyonId, int intervalInSeconds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"lokasyon_{lokasyonId}");
            _periodicService.AddOrUpdateLokasyon(int.Parse(lokasyonId), intervalInSeconds);
        }

        public async Task LeaveLocationGroup(string lokasyonId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lokasyon_{lokasyonId}");
            _periodicService.RemoveLokasyon(int.Parse(lokasyonId));
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                foreach (var kvp in _periodicService.GetActiveLokasyonlar())
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lokasyon_{kvp.Key}");
                    _periodicService.RemoveLokasyon(kvp.Key); // bağlantı kopunca user count eksilsin
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}