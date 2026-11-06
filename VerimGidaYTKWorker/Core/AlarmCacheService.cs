using KumesScada.Domain.Models;
using KumesScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace VerimGidaYTKWorker.Core
{
    public class AlarmCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly KumesScadaDbContext _kumesScadaDbContext;

        public AlarmCacheService(IMemoryCache memoryCache, KumesScadaDbContext kumesScadaDbContext)
        {
            _memoryCache = memoryCache;
            _kumesScadaDbContext = kumesScadaDbContext ?? throw new ArgumentNullException(nameof(kumesScadaDbContext)); // Null kontrolü
        }

        #region Alarm Cache
        private string GetAlarmCacheKeyForKonveyor(int konveyorId) => $"ActiveAlarms_Konveyor_{konveyorId.ToString()}";
        public List<AlarmLog> GetAlarmLogFromCache(int konveyorId)
        {
            string cacheKey = GetAlarmCacheKeyForKonveyor(konveyorId);
            _memoryCache.TryGetValue(cacheKey, out List<AlarmLog> alarmLogs);
            return alarmLogs;
        }

        public void SetKonveyorToAlarmCache(int kumesId, List<AlarmLog> alarmLogs)
        {
            string cacheKey = GetAlarmCacheKeyForKonveyor(kumesId);
            _memoryCache.Set(cacheKey, alarmLogs, TimeSpan.FromMinutes(10));
        }

        public async Task<List<AlarmLog>> GetAlarmLogsAsync(int konveyorId/*, DateTime now*/)
        {
            var cachedAlarmLogs = new List<AlarmLog>();
            string cacheKey = GetAlarmCacheKeyForKonveyor(konveyorId);

            // Cache'ten kontrol et
            var alarmLogs = GetAlarmLogFromCache(konveyorId);
            if (alarmLogs != null)
            {
                return alarmLogs;
            }

            // Cache'te yoksa API'den çek ve ekle
            alarmLogs = await GetActiveAlarmsByKonveyorLokasyonId(konveyorId/*, now*/);
            if (alarmLogs != null)
            {
                SetKonveyorToAlarmCache(konveyorId, alarmLogs);
            }

            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetActiveAlarmsByKonveyorLokasyonId(int konveyorId/*, DateTime now*/)
        {
            List<AlarmLog> activeAlarms = new List<AlarmLog>();
            activeAlarms = await _kumesScadaDbContext.AlarmLog.Where(x => x.KonveyorId == konveyorId /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Aktif != false && x.Silindi != true).ToListAsync();

            return activeAlarms;
        }
    
        public async Task SetAlarmPassive(int konveyorId, int alarmId/*, DateTime now*/)
        {
            var activeAlarm = await _kumesScadaDbContext.AlarmLog.Where(x => x.KonveyorId == konveyorId && x.AlarmId == alarmId/* && x.KayitZamani.Value.Date == now.Date*/ && x.Aktif != false && x.Silindi != true).FirstOrDefaultAsync();
            if (activeAlarm != null)
            {
                activeAlarm.Aktif = false;
                activeAlarm.PasifZamani = DateTime.Now;
            }
            await _kumesScadaDbContext.SaveChangesAsync();
            ClearMemoryCache(konveyorId);
        }

        public void ClearMemoryCache(int kumesId)
        {
            _memoryCache.Remove(GetAlarmCacheKeyForKonveyor(kumesId));
        }
        #endregion
    }
}
