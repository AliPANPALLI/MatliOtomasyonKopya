using PoultryHouseScada.API.SignalR.Test;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Domain.Models.ReportModels;
using Microsoft.AspNetCore.SignalR;

public class TestBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<TestHub> _hubContext;

    public TestBackgroundService(IServiceProvider serviceProvider, IHubContext<TestHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var raporService = scope.ServiceProvider.GetRequiredService<IKumesAppService>();
                    //var aktifPairs = TestTracker.KumesAlarmUserCounts.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();

                    //var death = new DeathStatsDto() { Today = 11, Yesterday = 100, Total10 = 10000, Trend = new List<double>() { 100, 120, 90, 80, 100, 120, 130, 90, 100, 105 } };
                    //var feed = new ResourceStatsDto() { Today = 13, TodayPer = 110, Total10 = 130, Total10Per = 1200, TrendTotal = new List<double>() { 100, 120, 90, 80, 100, 120, 110, 105, 95, 80 }, TrendPer = new List<double>() { 13, 12, 14, 12, 13, 11, 12, 14, 15, 13 } };
                    //var water = new ResourceStatsDto() { Today = 13, TodayPer = 110, Total10 = 130, Total10Per = 1200, TrendTotal = new List<double>() { 100, 120, 90, 80, 100, 120, 110, 105, 95, 80 }, TrendPer = new List<double>() { 13, 12, 14, 12, 13, 11, 12, 14, 15, 13 } };
                    //var egg = new EggStatsDto() { Today = 1712512, TodayPer = 0.85, Total10 = 10712512, Total10Per = 8, Trend = new List<double>() { 1712512, 1712412, 1712312, 1712512, 1712412, 1712612, 1712512, 1712412, 1712312, 1712512 } };

                    //foreach (var kumesId in aktifPairs)
                    //{
                    //    List<FarmDataDto> farmDataDtos = new List<FarmDataDto>();
                    //    farmDataDtos.Add(new FarmDataDto() { Id = "0", Name = "Matlı Çiftlik", Pop = 5812553, Stock = "980.5", Death = death, Water = water, Feed = feed, Egg = egg });
                    //    farmDataDtos.Add(new FarmDataDto() { Id = "1", Name = "Rahmiye Çiftlik", Pop = 1812553, Stock = "990.5", Death = death, Water = water, Feed = feed, Egg = egg });
                    //    farmDataDtos.Add(new FarmDataDto() { Id = "2", Name = "Merkez Çiftlik", Pop = 1612553, Stock = "987", Death = death, Water = water, Feed = feed, Egg = egg });
                    //    farmDataDtos.Add(new FarmDataDto() { Id = "3", Name = "Verim Gıda", Pop = 1412553, Stock = "989", Death = death, Water = water, Feed = feed, Egg = egg });
                    //    farmDataDtos.Add(new FarmDataDto() { Id = "4", Name = "KVZ", Pop = 1512553, Stock = "971", Death = death, Water = water, Feed = feed, Egg = egg });
                    //}
                    var kumesId = 3;
                    var result = await raporService.GetFarmDataDto();
                    await _hubContext.Clients.Group($"Test_kumesTip_{kumesId}").SendAsync("KumesAlarmData", result, stoppingToken);

                    var damizlikKumesId = 2;
                    var damizlikResult = await raporService.GetDamizlikFarmDataDto();
                    await _hubContext.Clients.Group($"Test_kumesTip_{damizlikKumesId}").SendAsync("KumesAlarmData", damizlikResult, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KumesAlarmBackgroundService error: {ex}");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}