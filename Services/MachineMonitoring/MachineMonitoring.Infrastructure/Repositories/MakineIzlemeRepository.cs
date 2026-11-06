using MachineMonitoring.Domain.Interfaces;
using MachineMonitoring.Domain.Models.ReportModels;
using Microsoft.EntityFrameworkCore;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;

namespace MachineMonitoring.Infrastructure.Repositories
{
    public class MakineIzlemeRepository : IMakineIzlemeRepository
    {
        private readonly MakineIzlemeDbContext _context;
        public MakineIzlemeRepository(MakineIzlemeDbContext context)
        {
            _context = context;
        }
        public async Task<List<MachineDashboardDto>> BuildDashboardData(int bolumId, CancellationToken ct)
        {
            var now = DateTime.Now;
            var todayStart = now.Date;
            var rangeStart = todayStart.AddDays(-9); // son 10 gün (bugün dahil)

            // 1) Makine listesi
            var machines = await _context.Makine.Where(x => x.BolumId == bolumId).Select(m => new { m.Id, m.Name, m.Connected, m.State }).ToListAsync(ct);

            var machineIds = machines.Select(x => x.Id).ToArray();

            // 2) Son state (en son log)
            // Not: Büyük tabloda daha optimize bir SQL gerekebilir; bu basit/iş görür örnek
            //var lastStates = await _context.MakineLog
            //    .Where(x => machineIds.Contains(x.MakineId))
            //    .GroupBy(x => x.MakineId)
            //    .Select(g => g.OrderByDescending(x => x.KayitZamani)
            //                  .Select(x => new { x.MakineId, x.State })
            //                  .FirstOrDefault()!)
            //    .ToListAsync(ct);
            var machineStateMap = machines.ToDictionary(x => x.Id, x => MachineStateMap.ToText(x.State));

            //var lastStateMap = lastStates
            //    .Where(x => x != null)
            //    .ToDictionary(x => x.MakineId, x => MachineStateMap.ToText(x.State));
            var lastLogs = await _context.MakineLog.Where(x => machineIds.Contains(x.MakineId)).GroupBy(x => x.MakineId).Select(g => g.OrderByDescending(x => x.KayitZamani).Select(x => new { x.MakineId, x.State, x.KayitZamani }).FirstOrDefault()!).ToListAsync(ct);

            //// 1) Ekranda "State" göstermek için (string map)
            //var lastStateMap = lastLogs.Where(x => x != null).ToDictionary(x => x.MakineId, x => MachineStateMap.ToText(x.State));

            // 2) Ongoing süreyi eklemek için (KayitZamani lazım)
            var lastLogMap = lastLogs.Where(x => x != null).ToDictionary(x => x.MakineId, x => x);

            // 3) Son 10 gün + bugün logları (DeltaTimeTicks ile süre hesap)
            var logs = await _context.MakineLog.Where(x => machineIds.Contains(x.MakineId) && x.KayitZamani >= rangeStart).Select(x => new { x.MakineId, x.State, x.KayitZamani, x.DeltaTime }).ToListAsync(ct);

            // helper: makine->(day->state->ticks toplam)
            var dayBuckets = new Dictionary<int, Dictionary<DateTime, (long run, long stop, long error)>>();
            foreach (var id in machineIds)
                dayBuckets[id] = new Dictionary<DateTime, (long run, long stop, long error)>();

            foreach (var l in logs)
            {
                var day = l.KayitZamani.Date;
                if (!dayBuckets[l.MakineId].TryGetValue(day, out var agg))
                    agg = (0, 0, 0);

                if (MachineStateMap.ToText(l.State) == "Run") agg.run += l.DeltaTime;
                else if (MachineStateMap.ToText(l.State) == "Error") agg.error += l.DeltaTime;
                else agg.stop += l.DeltaTime;

                dayBuckets[l.MakineId][day] = agg;
            }

            foreach (var kvp in lastLogMap)
            {
                var makineId = kvp.Key;
                var last = kvp.Value;

                var stateText = machineStateMap.TryGetValue(makineId, out var st) ? st : "Stop";

                // Son kayıttan şimdiye kadar devam eden süreyi ekle
                AddOngoingTicks(dayBuckets, makineId, stateText, last.KayitZamani, now);
            }

            // 5) DTO üret
            var result = new List<MachineDashboardDto>(machines.Count);

            foreach (var m in machines)
            {
                // today ticks
                dayBuckets[m.Id].TryGetValue(todayStart, out var todayAgg);

                var dto = new MachineDashboardDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    State = MachineStateMap.ToText(m.State), //lastStateMap.TryGetValue(m.Id, out var st) ? st : "Stop",
                    IsConnected = m.Connected,
                    Today = new TodayDurationsDto
                    {
                        Run = FormatHms(TimeSpan.FromTicks(todayAgg.run)),
                        Stop = FormatHms(TimeSpan.FromTicks(todayAgg.stop)),
                        Error = FormatHms(TimeSpan.FromTicks(todayAgg.error)),
                    },
                    History = new HistoryDurationsDto()
                };

                // history: son 10 gün (rangeStart..today)
                for (int i = 0; i < 10; i++)
                {
                    var d = rangeStart.AddDays(i).Date;
                    dayBuckets[m.Id].TryGetValue(d, out var a);

                    dto.History.Run.Add((int)ToHours(a.run));
                    dto.History.Stop.Add((int)ToHours(a.stop));
                    dto.History.Error.Add((int)ToHours(a.error));
                }
                result.Add(dto);
            }
            return result;
        }

        public async Task<List<MachineLogRowDto>> GetMachineLogs(int machineId, int take)
        {
            // Modal için örnek: en son N log
            var rows = await _context.MakineLog.Where(x => x.MakineId == machineId).OrderByDescending(x => x.KayitZamani).Take(take)
                .Select(x => new MachineLogRowDto
                {
                    Date = x.KayitZamani.ToString("dd.MM.yyyy"),
                    Time = x.KayitZamani.ToString("HH:mm"),
                    Status = MachineStateMap.ToText(x.State),
                    Duration = (int)Math.Round(TimeSpan.FromTicks(x.DeltaTime).TotalMinutes),
                    Desc = x.Aciklama ?? ""
                }).ToListAsync();

            return rows;
        }

        private static double ToHours(long ticks)
            => Math.Round(TimeSpan.FromTicks(ticks).TotalHours, 2);

        // 24 saati aşarsa da düzgün gözüksün diye total hour ile basıyoruz
        private static string FormatHms(TimeSpan ts)
        {
            var totalHours = (long)ts.TotalHours;
            return $"{totalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
        }

        static void AddOngoingTicks(Dictionary<int, Dictionary<DateTime, (long run, long stop, long error)>> dayBuckets, int makineId, string stateText, DateTime from, DateTime to)
        {
            if (to <= from) return;

            var cursor = from;

            while (cursor.Date < to.Date)
            {
                var endOfDay = cursor.Date.AddDays(1); // 00:00
                var ticks = (endOfDay - cursor).Ticks;
                AddTicksToDay(dayBuckets, makineId, cursor.Date, stateText, ticks);
                cursor = endOfDay;
            }

            // son gün parçası
            var lastTicks = (to - cursor).Ticks;
            AddTicksToDay(dayBuckets, makineId, cursor.Date, stateText, lastTicks);
        }

        static void AddTicksToDay(Dictionary<int, Dictionary<DateTime, (long run, long stop, long error)>> dayBuckets, int makineId, DateTime day, string stateText, long ticks)
        {
            if (!dayBuckets[makineId].TryGetValue(day, out var agg))
                agg = (0, 0, 0);

            if (stateText == "Run") agg.run += ticks;
            else if (stateText == "Error") agg.error += ticks;
            else agg.stop += ticks;

            dayBuckets[makineId][day] = agg;
        }
    }
    public static class MachineStateMap
    {
        public static string ToText(byte state)
            => state switch
            {
                (byte)MakineIzlemeDurum.Run => "Run",
                (byte)MakineIzlemeDurum.Error => "Error",
                _ => "Stop"
            };
    }
}
