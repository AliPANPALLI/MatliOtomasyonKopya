using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using SuHatScada.Application.Interfaces;
using System;
using System.Collections.Concurrent;

namespace SuHatScada.API
{
    public class VeriGuncellemeExpertBackgroundService2 : BackgroundService
    {
        private readonly IHubContext<SignalRHubExpert> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;

        // Key: (lokasyonId, userId)
        private readonly ConcurrentDictionary<(int lokasyonId, string userId), int> _subscriptions = new();
        private readonly Dictionary<(int, string), DateTime> _lastSent = new();

        public VeriGuncellemeExpertBackgroundService2(IHubContext<SignalRHubExpert> hubContext, IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        public void AddOrUpdateLokasyonUser(int lokasyonId, string userId, int intervalInSeconds)
        {
            if (lokasyonId <= 0 || string.IsNullOrWhiteSpace(userId))
                return;

            _subscriptions[(lokasyonId, userId)] = intervalInSeconds;
        }

        public void RemoveLokasyonUser(int lokasyonId, string userId)
        {
            _subscriptions.TryRemove((lokasyonId, userId), out _);
            _lastSent.Remove((lokasyonId, userId));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("[ExpertService] Servis başladı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var cizimAppService = scope.ServiceProvider.GetRequiredService<ICizimAppService>();

                    foreach (var ((lokasyonId, userId), interval) in _subscriptions)
                    {
                        if (lokasyonId <= 0 || string.IsNullOrWhiteSpace(userId))
                            continue;

                        var key = (lokasyonId, userId);
                        if (!_lastSent.TryGetValue(key, out var lastTime) ||
                            (DateTime.UtcNow - lastTime).TotalSeconds >= interval)
                        {
                            try
                            {
                                Console.WriteLine($"[ExpertService] Veri çekiliyor: Lokasyon={lokasyonId}, User={userId}");
                                var elements = await cizimAppService.GetScadaDataByLokasyonIdAndUserId(lokasyonId, userId);

                                var groupName = $"lokasyon_{lokasyonId}_user_{userId}";

                                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                                cts.CancelAfter(TimeSpan.FromSeconds(2)); // en fazla 2 sn bekle

                                await _hubContext.Clients.Group(groupName)
                                    .SendAsync($"receiveCizimData_{lokasyonId}", elements, cts.Token);

                                _lastSent[key] = DateTime.UtcNow;
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine($"[ExpertService] Gönderim zaman aşımına uğradı: Lokasyon={lokasyonId}, User={userId}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ExpertService] Hata: Lokasyon={lokasyonId}, User={userId} - {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExpertService] Genel döngü hatası: {ex.Message}");
                }

                try
                {
                    await Task.Delay(1000, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("[ExpertService] Task.Delay iptal edildi.");
                }
            }

            Console.WriteLine("[ExpertService] Servis durduruluyor.");
        }
    }

}