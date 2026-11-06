using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KumesScada.API.SignalR.KumesScada
{
    //[Authorize]
    public class SignalRHub : Hub
    {
        private readonly VeriGuncellemeBackgroundService _periodicService;

        public SignalRHub(VeriGuncellemeBackgroundService periodicService)
        {
            _periodicService = periodicService;
        }

        //public override async Task OnConnectedAsync()
        //{
        //    var httpContext = Context.GetHttpContext();
        //    var connectionId = Context.ConnectionId;
        //    var token = httpContext?.Request.Query["access_token"].ToString();
        //    var id = httpContext?.Request.Query["id"].ToString();

        //    Console.WriteLine($"[SignalR] Yeni bağlantı - ConnId: {connectionId}, id={id}, token={token?.Substring(0, 20)}...");

        //    await base.OnConnectedAsync();
        //}
        public async Task JoinKumesGroup(string kumesId, int intervalInSeconds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"kumes_{kumesId}");
            //Context.Items["kumesId"] = kumesId; // Disconnect'te temizlenmesi için
            _periodicService.AddOrUpdateKumes(int.Parse(kumesId), intervalInSeconds);
        }

        public async Task LeaveKumesGroup(string kumesId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kumes_{kumesId}");
            _periodicService.RemoveKumes(int.Parse(kumesId));
        }

        public async Task JoinKonveyorGroup(string konveyorId, int intervalInSeconds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"konveyor_{konveyorId}");
            //Context.Items["konveyorId"] = konveyorId; // Disconnect'te temizlenmesi için
            _periodicService.AddOrUpdateKonveyor(int.Parse(konveyorId), intervalInSeconds);
        }

        public async Task LeaveKonveyorGroup(string konveyorId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"konveyor_{konveyorId}");
            _periodicService.RemoveKonveyor(int.Parse(konveyorId));
        }

        //public override async Task OnDisconnectedAsync(Exception? exception)
        //{
        //    Console.WriteLine($"[SignalR] Bağlantı koptu: {Context.ConnectionId}");

        //    // Dictionary güncelleme (aktif kümes / konveyörleri azalt)
        //    if (Context.Items.TryGetValue("kumesId", out var kid))
        //    {
        //        _periodicService.RemoveKumes(int.Parse(kid.ToString()));
        //    }
        //    if (Context.Items.TryGetValue("konveyorId", out var cid))
        //    {
        //        _periodicService.RemoveKonveyor(int.Parse(cid.ToString()));
        //    }

        //    await base.OnDisconnectedAsync(exception);
        //}


        //public override async Task OnDisconnectedAsync(Exception exception)
        //{
        //    // Bağlantı kapandığında gruptan çıkar
        //    foreach (var kumesId in _timersForKumes.Keys)
        //    {
        //        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"kumes_{kumesId}");
        //    }

        //    foreach (var konveyorId in _timersForKonveyor.Keys)
        //    {
        //        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"konveyor_{konveyorId}");
        //    }

        //    // Timer'ları durdur
        //    foreach (var kumesId in _timersForKumes.Keys)
        //    {
        //        StopPeriodicUpdates(kumesId.ToString());
        //    }

        //    foreach (var konveyorId in _timersForKonveyor.Keys)
        //    {
        //        StopPeriodicUpdatesForKonveyor(konveyorId.ToString());
        //    }

        //    await base.OnDisconnectedAsync(exception);
        //}
    }
}