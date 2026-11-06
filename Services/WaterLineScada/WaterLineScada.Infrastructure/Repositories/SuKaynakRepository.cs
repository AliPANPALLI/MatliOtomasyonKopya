using Microsoft.EntityFrameworkCore;
using WaterLineScada.Domain.DbViewModels;
using WaterLineScada.Domain.Interfaces;
using WaterLineScada.Domain.Models;
using WaterLineScada.Domain.Models.ReportModels;
using WaterLineScada.Infrastructure.Repositories.Core;
using System.Globalization;

namespace WaterLineScada.Infrastructure.Repositories
{
    public class SuKaynakRepository : Repository<SuKaynak>, ISuKaynakRepository
    {
        private readonly SuHatScadaDbContext _context;
        public SuKaynakRepository(SuHatScadaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Cizim>> GetAllCizimForYetki()
        {
            var result = await _context.Cizim.Where(x => x.Silindi != true).ToListAsync();
            return result;
        }

        public async Task<List<vwCizimLokasyonEsleme>> GetAllSuKaynakCizimForYetki()
        {
            var result = await _context.vwCizimLokasyonEsleme.Where(x => x.Silindi != true).ToListAsync();
            return result;
        }

        public async Task<List<vwAlarmLog>> GetAktifAlarmLogForSuIzleme(int cizimId)
        {
            var cizimLokasyonlar = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.CizimId == cizimId && x.Silindi != true).Select(x => x.LokasyonId).ToListAsync();
            var alarmLogs = await _context.vwAlarmLog.Where(x => cizimLokasyonlar.Contains(x.LokasyonId.GetValueOrDefault(0)) && x.Aktif != false /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Silindi != true && x.AlarmScadaEkrandaGosterilsinMi != false).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<vwAlarmLog>> GetPasifAlarmLogForSuIzleme(int cizimId)
        {
            var cizimLokasyonlar = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.CizimId == cizimId && x.Silindi != true).Select(x => x.LokasyonId).ToListAsync();
            var alarmLogs = await _context.vwAlarmLog.Where(x => cizimLokasyonlar.Contains(x.LokasyonId.GetValueOrDefault(0)) && x.Aktif == false && x.KayitZamani.Value.Date >= DateTime.Now.Date.AddDays(-10) && x.Silindi != true && x.AlarmScadaEkrandaGosterilsinMi != false).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetAktifAlarmLogForSuIzlemeByLokasyonId(int lokasyonId)
        {
            var cizimId = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.Silindi != true).Select(x => x.CizimId).FirstOrDefaultAsync();
            var cizimLokasyonlar = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.CizimId == cizimId && x.Silindi != true).Select(x => x.LokasyonId).ToListAsync();
            var alarmLogs = await _context.AlarmLog.Where(x => cizimLokasyonlar.Contains(x.LokasyonId.GetValueOrDefault(0)) && x.Aktif != false /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Silindi != true).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetAlarmLogForSuIzleme(int lokasyonId)
        {
            var cizimId = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.Silindi != true).Select(x => x.CizimId).FirstOrDefaultAsync();
            var cizimLokasyonlar = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.CizimId == cizimId && x.Silindi != true).Select(x => x.LokasyonId).ToListAsync();

            var alarmLogs = await _context.AlarmLog.Where(x => cizimLokasyonlar.Contains(x.LokasyonId.GetValueOrDefault(0)) /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Silindi != true).OrderByDescending(x => x.KayitZamani).ToListAsync();
            return alarmLogs;
        }

        public async Task<List<CizimLokasyonEsleme>> GetCizimLokasyonEslemeFromLokasyonId(int lokasyonId)
        {
            var cizimLokasyonEsleme = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.LokasyonId == lokasyonId && x.Silindi != true).FirstOrDefaultAsync();
            if (cizimLokasyonEsleme == null)
                return new List<CizimLokasyonEsleme>();
            var cizimLokasyonEslemeler = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.CizimId == cizimLokasyonEsleme.CizimId && x.Silindi != true).ToListAsync();

            return cizimLokasyonEslemeler;
        }

        public async Task<List<CizimLokasyonEsleme>> GetCizimLokasyonEslemeFromCizimId(int cizimId)
        {
            var cizimLokasyonEslemeler = await _context.CizimLokasyonEsleme.AsNoTracking().Where(x => x.CizimId == cizimId && x.Silindi != true).ToListAsync();
            return cizimLokasyonEslemeler;
        }
        public async Task<List<vwCizimLokasyonEsleme>> GetVwCizimLokasyonEslemeFromCizimId(int cizimId)
        {
            var cizimLokasyonEslemeler = await _context.vwCizimLokasyonEsleme.AsNoTracking().Where(x => x.CizimId == cizimId && x.Silindi != true).ToListAsync();
            return cizimLokasyonEslemeler;
        }
        //public async Task<List<EtiketLog>> GetVerilerFromSuKaynakId(List<int> suKaynakIds)//todo baslangic bitis datetime eklenecek
        //{
        //    var veriler = await _context.EtiketLog.AsNoTracking().Where(x => suKaynakIds.Contains(x.SuKaynakId.GetValueOrDefault(0)) && x.KayitZamani.Value.Date == DateTime.Now.Date).ToListAsync();
        //    return veriler;
        //}
        public async Task<List<EtiketLogYeni>> GetVerilerFromSuKaynakId(List<int> suKaynakIds)//todo baslangic bitis datetime eklenecek
        {
            var veriler = await _context.EtiketLogYeni.AsNoTracking().Where(x => suKaynakIds.Contains(x.SuKaynakId.GetValueOrDefault(0)) && x.KayitZamani.Value.Date == DateTime.Now.Date).ToListAsync();
            return veriler;
        }

        public async Task<List<TimePeriodData>> GetToplamSuDataFromViewByVwSaatlik(string periyot, int sayacId, DateTime startDate, DateTime endDate)
        {
            DateTime baslangicZamani = startDate;
            DateTime bitisZamani = endDate;
            switch (periyot)
            {
                case "Günlük":
                    bitisZamani = endDate.AddDays(1);
                    break;
                case "Haftalık":
                    DateTime weekStart = startDate.AddDays(-((int)startDate.DayOfWeek - (int)DayOfWeek.Monday));
                    DateTime weekEnd = endDate.AddDays(-((int)endDate.DayOfWeek - (int)DayOfWeek.Monday));
                    baslangicZamani = weekStart;
                    bitisZamani = weekEnd;
                    break;
                case "Aylık":
                    DateTime monthStart = new DateTime(startDate.Year, startDate.Month, 1);
                    // Extend to the end of the month of the provided endDate (inclusive)
                    DateTime monthEnd = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);
                    baslangicZamani = monthStart;
                    bitisZamani = monthEnd;
                    break;
                case "Yıllık":
                    DateTime yearStart = new DateTime(startDate.Year, 1, 1);
                    DateTime yearEnd = new DateTime(startDate.Year, 12, 31);
                    baslangicZamani = yearStart;
                    bitisZamani = yearEnd;
                    break;
                default:
                    break;
            }
            var saatlikVeri = await _context.vwSuKaynakLog_Saatlik
       .Where(x => x.SayacId == sayacId && x.IlkZaman >= baslangicZamani && x.SonZaman <= bitisZamani)
       .ToListAsync();

            switch (periyot)
            {
                case "Saatlik":
                    return saatlikVeri
                        .OrderBy(x => x.IlkZaman)
                        .Select(x => new TimePeriodData
                        {
                            Periyot = x.IlkZaman.Value,
                            Toplam = x.DebiFark.GetValueOrDefault(0),
                            PeriyotName = x.IlkZaman.Value.ToString("dd MM yyyy / HH:00", new CultureInfo("tr-TR")) + "-" + x.IlkZaman.Value.AddHours(1).ToString("HH:00")
                        })
                        .ToList();

                case "Günlük":
                    return saatlikVeri
                        .GroupBy(x => x.IlkZaman.Value.Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new TimePeriodData
                        {
                            Periyot = g.Key,
                            Toplam = g.Sum(x => x.DebiFark.GetValueOrDefault(0)),
                            PeriyotName = g.Key.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR"))
                        })
                        .ToList();

                case "Haftalık":
                    return saatlikVeri
                        .GroupBy(x =>
                        {
                            var dt = x.IlkZaman.Value;
                            var calendar = CultureInfo.InvariantCulture.Calendar;
                            int week = calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                            return new { dt.Year, Hafta = week };
                        })
                        .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Hafta)
                        .Select(g => new TimePeriodData
                        {
                            Periyot = g.Min(x => x.IlkZaman.Value),
                            Toplam = g.Sum(x => x.DebiFark.GetValueOrDefault(0)),
                            PeriyotName = $"{g.Key.Year} / Hafta {g.Key.Hafta}"
                        })
                        .ToList();

                case "Aylık":
                    return saatlikVeri
                        .GroupBy(x => new { x.IlkZaman.Value.Year, x.IlkZaman.Value.Month })
                        .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                        .Select(g => new TimePeriodData
                        {
                            Periyot = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Toplam = g.Sum(x => x.DebiFark.GetValueOrDefault(0)),
                            PeriyotName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", new CultureInfo("tr-TR"))
                        })
                        .ToList();

                case "Yıllık":
                    return saatlikVeri
                        .GroupBy(x => x.IlkZaman.Value.Year)
                        .OrderBy(g => g.Key)
                        .Select(g => new TimePeriodData
                        {
                            Periyot = new DateTime(g.Key, 1, 1),
                            Toplam = g.Sum(x => x.DebiFark.GetValueOrDefault(0)),
                            PeriyotName = g.Key.ToString()
                        })
                        .ToList();

                default:
                    throw new ArgumentException("Geçersiz periyot");
            }
        }

        public async Task<List<TimePeriodData>> GetToplamSuDataFromView(string periyot, int sayacId, DateTime startDate, DateTime endDate)
        {
            switch (periyot)
            {
                case "Saatlik":
                    return await _context.vwSuKaynakLog_Saatlik
                        .Where(x => x.SayacId == sayacId && ((x.IlkZaman >= startDate && x.SonZaman <= endDate) || (x.IlkZaman >= endDate && x.SonZaman <= endDate) || (x.IlkZaman >= startDate && x.SonZaman <= startDate)))
                        .OrderBy(x => x.IlkZaman)
                        .Select(x => new TimePeriodData
                        {
                            Periyot = x.IlkZaman.Value,
                            Toplam = x.DebiFark.GetValueOrDefault(0),
                            //PeriyotName = x.Tarih.Value.ToString("dd MM yyyy", new CultureInfo("tr-TR")) + " / " + x.IlkZaman.Value.Hour.ToString("D2") + ":" + x.IlkZaman.Value.Minute.ToString("D2") + "-" + x.SonZaman.Value.Hour.ToString("D2") + ":" + x.SonZaman.Value.Minute.ToString("D2"),
                            PeriyotName = x.IlkZaman.Value.ToString("dd MM yyyy", new CultureInfo("tr-TR")) + " / " + x.IlkZaman.Value.Hour.ToString("D2") + ":00" + /*hourlyData.First()?.KayitZamani.Value.Minute.ToString("D2") +*/ "-" + x.IlkZaman.Value.AddHours(1).Hour.ToString("D2") + ":00" /*+ hourlyData.Last()?.KayitZamani.Value.Minute.ToString("D2")*/
                        })
                        .ToListAsync();

                case "Günlük":
                    endDate = endDate.AddDays(1);
                    return await _context.vwSuKaynakLog_Gunluk
                        .Where(x => x.SayacId == sayacId && x.IlkZaman >= startDate && x.SonZaman <= endDate)
                        .OrderBy(x => x.IlkZaman)
                        .Select(x => new TimePeriodData
                        {
                            Periyot = x.IlkZaman.Value,
                            Toplam = x.DebiFark.GetValueOrDefault(0),
                            PeriyotName = x.Tarih.Value.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR")),
                        })
                        .ToListAsync();

                case "Haftalık":
                    DateTime weekStart = startDate.AddDays(-((int)startDate.DayOfWeek - (int)DayOfWeek.Monday));
                    DateTime weekEnd = endDate.AddDays(-((int)endDate.DayOfWeek - (int)DayOfWeek.Monday));

                    return await _context.vwSuKaynakLog_Haftalik
                        .Where(x => x.SayacId == sayacId && x.IlkZaman >= weekStart && x.SonZaman <= weekEnd)
                        .OrderBy(x => x.IlkZaman)
                        .Select(x => new TimePeriodData
                        {
                            Periyot = x.IlkZaman.Value,
                            Toplam = x.DebiFark.GetValueOrDefault(0),
                            PeriyotName = x.Yil.ToString() + " / Hafta " + x.Hafta.ToString(),
                        })
                        .ToListAsync();

                case "Aylık":
                    DateTime monthStart = new DateTime(startDate.Year, startDate.Month, 1);
                    DateTime monthEnd = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);
                    return await _context.vwSuKaynakLog_Aylik
                        .Where(x => x.SayacId == sayacId && x.IlkZaman >= monthStart && x.SonZaman <= monthEnd)
                        .OrderBy(x => x.IlkZaman)
                        .Select(x => new TimePeriodData
                        {
                            Periyot = x.IlkZaman.Value,
                            Toplam = x.DebiFark.GetValueOrDefault(0),
                            PeriyotName = x.IlkZaman.Value.ToString("MMMM yyyy", new CultureInfo("tr-TR")), // "Ocak 2024",
                        })
                        .ToListAsync();

                case "Yıllık":
                    DateTime yearStart = new DateTime(startDate.Year, 1, 1);
                    DateTime yearEnd = new DateTime(startDate.Year, 12, 31);
                    return await _context.vwSuKaynakLog_Yillik
                        .Where(x => x.SayacId == sayacId && x.IlkZaman >= yearStart && x.SonZaman <= yearEnd)
                        .OrderBy(x => x.IlkZaman)
                        .Select(x => new TimePeriodData
                        {
                            Periyot = x.IlkZaman.Value,
                            Toplam = x.DebiFark.GetValueOrDefault(0),
                            PeriyotName = x.IlkZaman.Value.ToString("yyyy", new CultureInfo("tr-TR")),
                        })
                        .ToListAsync();

                default:
                    throw new ArgumentException("Geçersiz periyot");
            }
        }

        public async Task<List<TimePeriodData>> GetTankSuDataFromView(string periyot, int suKaynakId, string tankName, DateTime startDate, DateTime endDate)
        {
            DateTime baslangicZamani = startDate;
            DateTime bitisZamani = endDate;
            switch (periyot)
            {
                case "Günlük":
                    bitisZamani = endDate.AddDays(1);
                    break;
                case "Haftalık":
                    DateTime weekStart = startDate.AddDays(-((int)startDate.DayOfWeek - (int)DayOfWeek.Monday));
                    DateTime weekEnd = endDate.AddDays(-((int)endDate.DayOfWeek - (int)DayOfWeek.Monday));
                    baslangicZamani = weekStart;
                    bitisZamani = weekEnd;
                    break;
                case "Aylık":
                    DateTime monthStart = new DateTime(startDate.Year, startDate.Month, 1);
                    DateTime monthEnd = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);
                    baslangicZamani = monthStart;
                    bitisZamani = monthEnd;
                    break;
                case "Yıllık":
                    DateTime yearStart = new DateTime(startDate.Year, 1, 1);
                    DateTime yearEnd = new DateTime(startDate.Year, 12, 31);
                    baslangicZamani = yearStart;
                    bitisZamani = yearEnd;
                    break;
                default:
                    break;
            }

            var query = await _context.vwTankLog_Saatlik
                .Where(x => x.SuKaynakId == suKaynakId &&
                            x.IlkZaman >= baslangicZamani && x.SonZaman <= bitisZamani)
                .ToListAsync();

            switch (periyot)
            {
                case "Saatlik":
                    return query.SelectMany(x => new[]
                    {
                new TimePeriodData { Periyot = x.IlkZaman.Value, Toplam = x.MinHacim ?? 0, PeriyotName = x.IlkZaman.Value.ToString("dd MM yyyy", new CultureInfo("tr-TR")) + " / " + x.IlkZaman.Value.Hour.ToString("D2") + ":00" + /*hourlyData.First()?.KayitZamani.Value.Minute.ToString("D2") +*/ "-" + x.IlkZaman.Value.AddHours(1).Hour.ToString("D2") + ":00" /*+ hourlyData.Last()?.KayitZamani.Value.Minute.ToString("D2")*/
 },
                new TimePeriodData { Periyot = x.IlkZaman.Value, Toplam = x.OrtalamaHacim ?? 0, PeriyotName = x.IlkZaman.Value.ToString("dd MM yyyy", new CultureInfo("tr-TR")) + " / " + x.IlkZaman.Value.Hour.ToString("D2") + ":00" + /*hourlyData.First()?.KayitZamani.Value.Minute.ToString("D2") +*/ "-" + x.IlkZaman.Value.AddHours(1).Hour.ToString("D2") + ":00" /*+ hourlyData.Last()?.KayitZamani.Value.Minute.ToString("D2")*/
 },
                new TimePeriodData { Periyot = x.IlkZaman.Value, Toplam = x.MaxHacim ?? 0, PeriyotName = x.IlkZaman.Value.ToString("dd MM yyyy", new CultureInfo("tr-TR")) + " / " + x.IlkZaman.Value.Hour.ToString("D2") + ":00" + /*hourlyData.First()?.KayitZamani.Value.Minute.ToString("D2") +*/ "-" + x.IlkZaman.Value.AddHours(1).Hour.ToString("D2") + ":00" /*+ hourlyData.Last()?.KayitZamani.Value.Minute.ToString("D2")*/
 }
            }).ToList();

                case "Günlük":
                    return query
                        .GroupBy(x => x.Tarih)
                        .SelectMany(g => new[]
                        {
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Min(x => x.MinHacim) ?? 0, PeriyotName = g.Key.Value.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR")) },
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Average(x => x.OrtalamaHacim) ?? 0, PeriyotName = g.Key.Value.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR")) },
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Max(x => x.MaxHacim) ?? 0, PeriyotName = g.Key.Value.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR"))}
                        }).ToList();

                case "Haftalık":
                    var hafta = new CultureInfo("tr-TR").Calendar.GetWeekOfYear(startDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    return query
                        .GroupBy(x => System.Globalization.ISOWeek.GetWeekOfYear(x.Tarih.Value))
                        .SelectMany(g => new[]
                        {
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Min(x => x.MinHacim) ?? 0, PeriyotName = g.Min(x => x.IlkZaman.Value).Year.ToString() + " / Hafta " + hafta.ToString(),
},
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Average(x => x.OrtalamaHacim) ?? 0, PeriyotName = g.Min(x => x.IlkZaman.Value).Year.ToString() + " / Hafta " + hafta.ToString(),
 },
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Max(x => x.MaxHacim) ?? 0, PeriyotName = g.Min(x => x.IlkZaman.Value).Year.ToString() + " / Hafta " + hafta.ToString(),
 }
                        }).ToList();

                case "Aylık":
                    return query
                        .GroupBy(x => new { x.Tarih.Value.Year, x.Tarih.Value.Month })
                        .SelectMany(g => new[]
                        {
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Min(x => x.MinHacim) ?? 0, PeriyotName =  g.Min(x => x.IlkZaman.Value).ToString("MMMM yyyy", new CultureInfo("tr-TR")), },
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Average(x => x.OrtalamaHacim) ?? 0, PeriyotName =  g.Min(x => x.IlkZaman.Value).ToString("MMMM yyyy", new CultureInfo("tr-TR")), },
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Max(x => x.MaxHacim) ?? 0, PeriyotName =  g.Min(x => x.IlkZaman.Value).ToString("MMMM yyyy", new CultureInfo("tr-TR")), }
                        }).ToList();

                case "Yıllık":
                    return query
                        .GroupBy(x => x.Tarih.Value.Year)
                        .SelectMany(g => new[]
                        {
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Min(x => x.MinHacim) ?? 0, PeriyotName = g.Min(x => x.IlkZaman.Value).ToString("yyyy", new CultureInfo("tr-TR")) },
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Average(x => x.OrtalamaHacim) ?? 0, PeriyotName = g.Min(x => x.IlkZaman.Value).ToString("yyyy", new CultureInfo("tr-TR")) },
                    new TimePeriodData { Periyot = g.Min(x => x.IlkZaman.Value), Toplam = g.Max(x => x.MaxHacim) ?? 0, PeriyotName = g.Min(x => x.IlkZaman.Value).ToString("yyyy", new CultureInfo("tr-TR")) }
                        }).ToList();
            }
            throw new ArgumentException("Geçersiz periyot");
        }

        public async Task<List<vwLokasyon>> GetLokasyonlarFromLokasyonIds(List<int> lokasyonIds)
        {
            return await _context.vwLokasyon.Where(x => lokasyonIds.Contains(x.Id)).ToListAsync();
        }

        //public async Task<List<TimePeriodData>> GetToplamCalismaSuresiDataFromView(string periyot, int suKaynakId, DateTime startDate, DateTime endDate)
        //{
        //    switch (periyot)
        //    {
        //        case "Saatlik":
        //            return await _context.vwCalismaSure_Saatlik
        //                .Where(x => x.SuKaynakId == suKaynakId && ((x.IlkZaman >= startDate && x.SonZaman <= endDate) || (x.IlkZaman >= endDate && x.SonZaman <= endDate) || (x.IlkZaman >= startDate && x.SonZaman <= startDate)))
        //                .OrderBy(x => x.IlkZaman)
        //                .Select(x => new TimePeriodData
        //                {
        //                    Periyot = x.IlkZaman.Value,
        //                    Toplam = x.ToplamSaat.GetValueOrDefault(0),
        //                    PeriyotName = x.IlkZaman.Value.ToString("dd MM yyyy", new CultureInfo("tr-TR")) + " / " + x.IlkZaman.Value.Hour.ToString("D2") + ":00" + /*hourlyData.First()?.KayitZamani.Value.Minute.ToString("D2") +*/ "-" + x.IlkZaman.Value.AddHours(1).Hour.ToString("D2") + ":00" /*+ hourlyData.Last()?.KayitZamani.Value.Minute.ToString("D2")*/
        //                })
        //                .ToListAsync();

        //        case "Günlük":
        //            endDate = endDate.AddDays(1);
        //            return await _context.vwCalismaSure_Gunluk
        //                .Where(x => x.SuKaynakId == suKaynakId && x.IlkZaman >= startDate && x.SonZaman <= endDate)
        //                .OrderBy(x => x.IlkZaman)
        //                .Select(x => new TimePeriodData
        //                {
        //                    Periyot = x.IlkZaman.Value,
        //                    Toplam = x.ToplamSaat.GetValueOrDefault(0),
        //                    PeriyotName = x.IlkZaman.Value.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR")),
        //                })
        //                .ToListAsync();

        //        case "Haftalık":
        //            DateTime weekStart = startDate.AddDays(-((int)startDate.DayOfWeek - (int)DayOfWeek.Monday));
        //            DateTime weekEnd = endDate.AddDays(-((int)endDate.DayOfWeek - (int)DayOfWeek.Monday));
        //            var hafta = new CultureInfo("tr-TR").Calendar.GetWeekOfYear(startDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        //            return await _context.vwCalismaSure_Haftalik
        //                .Where(x => x.SuKaynakId == suKaynakId && x.IlkZaman >= weekStart && x.SonZaman <= weekEnd)
        //                .OrderBy(x => x.IlkZaman)
        //                .Select(x => new TimePeriodData
        //                {
        //                    Periyot = x.IlkZaman.Value,
        //                    Toplam = x.ToplamSaat.GetValueOrDefault(0),
        //                    PeriyotName = x.IlkZaman.Value.Year.ToString() + " / Hafta " + hafta.ToString(),
        //                })
        //                .ToListAsync();

        //        case "Aylık":
        //            DateTime monthStart = new DateTime(startDate.Year, startDate.Month, 1);
        //            DateTime monthEnd = monthStart.AddMonths(1);
        //            return await _context.vwCalismaSure_Aylik
        //                .Where(x => x.SuKaynakId == suKaynakId && x.IlkZaman >= monthStart && x.SonZaman <= monthEnd)
        //                .OrderBy(x => x.IlkZaman)
        //                .Select(x => new TimePeriodData
        //                {
        //                    Periyot = x.IlkZaman.Value,
        //                    Toplam = x.ToplamSaat.GetValueOrDefault(0),
        //                    PeriyotName = x.IlkZaman.Value.ToString("MMMM yyyy", new CultureInfo("tr-TR")), // "Ocak 2024",
        //                })
        //                .ToListAsync();

        //        case "Yıllık":
        //            DateTime yearStart = new DateTime(startDate.Year, 1, 1);
        //            DateTime yearEnd = new DateTime(startDate.Year, 12, 31);
        //            return await _context.vwCalismaSure_Yillik
        //                .Where(x => x.SuKaynakId == suKaynakId && x.IlkZaman >= yearStart && x.SonZaman <= yearEnd)
        //                .OrderBy(x => x.IlkZaman)
        //                .Select(x => new TimePeriodData
        //                {
        //                    Periyot = x.IlkZaman.Value,
        //                    Toplam = x.ToplamSaat.GetValueOrDefault(0),
        //                    PeriyotName = x.IlkZaman.Value.ToString("yyyy", new CultureInfo("tr-TR")),
        //                })
        //                .ToListAsync();

        //        default:
        //            throw new ArgumentException("Geçersiz periyot");
        //    }
        //}
    }
}