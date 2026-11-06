using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WaterLineScada.Domain.Models;
using WaterLineScada.Infrastructure;

namespace SuIzlemeWorker.Core
{
    public class AlarmCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly SuHatScadaDbContext _suHatScadaDbContext;
        public AlarmCacheService(IMemoryCache memoryCache, SuHatScadaDbContext suHatScadaDbContext)
        {
            _memoryCache = memoryCache;
            _suHatScadaDbContext = suHatScadaDbContext ?? throw new ArgumentNullException(nameof(suHatScadaDbContext)); // Null kontrolü
        }

        public async Task<List<int>> GetSuLokasyonIds()
        {
            var suLokasyonIds = await _suHatScadaDbContext.PlcAyarlar.Where(x => x.Aktif != false && x.Silindi != true).Select(x => x.LokasyonId.GetValueOrDefault(0)).ToListAsync();
            return suLokasyonIds;
        }

        private string GetCacheKeyForLokasyon(int lokasyonId) => $"ActiveAlarms_Lokasyon_{lokasyonId.ToString()}";
        public List<AlarmLog> GetAlarmLogFromCache(int lokasyonId)
        {
            string cacheKey = GetCacheKeyForLokasyon(lokasyonId);
            _memoryCache.TryGetValue(cacheKey, out List<AlarmLog> alarmLogs);
            return alarmLogs;
        }

        public void SetLokasyonToAlarmCache(int lokasyonId, List<AlarmLog> alarmLogs)
        {
            string cacheKey = GetCacheKeyForLokasyon(lokasyonId);
            _memoryCache.Set(cacheKey, alarmLogs, TimeSpan.FromMinutes(10));
        }

        public async Task<List<AlarmLog>> GetAlarmLogsAsync(int lokasyonId/*, DateTime now*/)
        {
            var cachedAlarmLogs = new List<AlarmLog>();
            string cacheKey = GetCacheKeyForLokasyon(lokasyonId);

            // Cache'ten kontrol et
            var alarmLogs = GetAlarmLogFromCache(lokasyonId);
            if (alarmLogs != null)
            {
                return alarmLogs;
            }

            // Cache'te yoksa API'den çek ve ekle
            alarmLogs = await GetActiveAlarmsByLokasyonId(lokasyonId/*, now*/);
            if (alarmLogs != null)
            {
                SetLokasyonToAlarmCache(lokasyonId, alarmLogs);
            }

            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetActiveAlarmsByLokasyonId(int lokasyonId/*, DateTime now*/)
        {
            List<AlarmLog> activeAlarms = new List<AlarmLog>();
            activeAlarms = await _suHatScadaDbContext.AlarmLog.Where(x => x.LokasyonId == lokasyonId /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Aktif != false && x.Silindi != true).ToListAsync();

            return activeAlarms;
        }

        //public async Task SetOldAlarmsPassiveByLokasyonId(int lokasyonId, DateTime now)
        //{
        //    var oldActiveAlarms = await _suHatScadaDbContext.AlarmLog.Where(x => x.LokasyonId == lokasyonId && x.KayitZamani.Value.Date < now.Date && x.Aktif != false && x.Silindi != true).ToListAsync();
        //    var lokasyonIds = oldActiveAlarms.Select(x => x.LokasyonId.GetValueOrDefault(0)).Distinct().ToList();
        //    foreach (var oldAlarm in oldActiveAlarms)
        //    {
        //        oldAlarm.Aktif = false;
        //        oldAlarm.PasifZamani = now;
        //    }
        //    await _suHatScadaDbContext.SaveChangesAsync();
        //    ClearMemoryCache(lokasyonId);
        //}
        public async Task SetAlarmPassive(int lokasyonId, int alarmLogTipId, byte alarmSeviye/*, DateTime now*/)
        {
            //var oldActiveAlarms = await _suHatScadaDbContext.AlarmLog.Where(x => x.KayitZamani.Value.Date < now.Date && x.Aktif != false && x.Silindi != true).ToListAsync();
            //foreach (var oldAlarm in oldActiveAlarms)
            //{
            //    oldAlarm.Aktif = false;
            //    oldAlarm.PasifZamani = now;
            //}

            var activeAlarm = await _suHatScadaDbContext.AlarmLog.Where(x => x.LokasyonId == lokasyonId && x.AlarmEnum == alarmLogTipId && x.AlarmSeviye == alarmSeviye /*&& x.KayitZamani.Value.Date == now.Date*/ && x.Aktif != false && x.Silindi != true).FirstOrDefaultAsync();
            if (activeAlarm != null)
            {
                activeAlarm.Aktif = false;
                activeAlarm.PasifZamani = DateTime.Now;
            }
            await _suHatScadaDbContext.SaveChangesAsync();

            ClearMemoryCache(lokasyonId);
        }

        public void ClearMemoryCache(int lokasyonId)
        {
            _memoryCache.Remove(GetCacheKeyForLokasyon(lokasyonId));
        }
    }
}