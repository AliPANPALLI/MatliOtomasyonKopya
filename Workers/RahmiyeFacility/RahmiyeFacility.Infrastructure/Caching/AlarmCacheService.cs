using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;

namespace RahmiyeFacility.Infrastructure.Caching
{
    public class AlarmCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly KumesScadaDbContext _kumesScadaDbContext;

        public AlarmCacheService(IMemoryCache memoryCache, KumesScadaDbContext kumesScadaDbContext)
        {
            _memoryCache = memoryCache;
            _kumesScadaDbContext = kumesScadaDbContext ?? throw new ArgumentNullException(nameof(kumesScadaDbContext));
        }

        public async Task<bool> GetOluHayvanSayisiGirildiMiByKumesId(int kumesId)
        {
            var donem = await _kumesScadaDbContext.Donem.Where(x => x.KumesId == kumesId && x.Aktif != false && x.Silindi != true && x.CikisTarihi == null).FirstOrDefaultAsync();
            if (donem == null)
            {
                return true;
            }
            var oluHayvanSayisiGirildiMi = await _kumesScadaDbContext.KumesHayvanLog.Where(x => x.KumesId == kumesId && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu && x.KayitZamani.Value.Date == DateTime.Now.Date && x.Silindi != true).FirstOrDefaultAsync() == null ? false : true;
            return oluHayvanSayisiGirildiMi;
        }

        public async Task<bool> GetHaftaSonuOluHayvanSayisiGirildiMiByKumesId(int kumesId)
        {
            var donem = await _kumesScadaDbContext.Donem.Where(x => x.KumesId == kumesId && x.Aktif != false && x.Silindi != true && x.CikisTarihi == null).FirstOrDefaultAsync();
            if (donem == null)
            {
                return true;
            }
            var haftaSonuOluHayvanSayisiGirildiMi = (await _kumesScadaDbContext.KumesHayvanLog.Where(x => x.KumesId == kumesId && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu && x.KayitZamani.Value.Date >= DateTime.Now.AddDays(-2).Date && x.KayitZamani.Value.Date < DateTime.Now.Date && x.Silindi != true).ToListAsync()).Count() >= 2 ? true : false;
            return haftaSonuOluHayvanSayisiGirildiMi;
        }

        public async Task<List<Kumes>> GetKumeslerAsync(int lokasyonId)
        {
            var kumesler = await _kumesScadaDbContext.Kumes.Where(x => x.LokasyonId == lokasyonId /*&& x.Aktif != false*/ && x.Silindi != true).ToListAsync();
            return kumesler;
        }

        #region Hayvan Sayısı Cache    
        public async Task<int> GetGunlukCanliHayvanSayisiByKumesId(int kumesId)
        {
            string cacheKey = $"GunlukHayvanSayisi_Kumes_{kumesId}";

            // Eğer cache varsa onu döndür
            if (_memoryCache.TryGetValue(cacheKey, out int cachedValue))
            {
                return cachedValue;
            }

            // Yoksa hesapla
            var donem = await _kumesScadaDbContext.Donem
                .Where(x => x.KumesId == kumesId && x.Aktif != false && x.Silindi != true && x.CikisTarihi == null)
                .FirstOrDefaultAsync();

            if (donem == null)
            {
                return 0;
            }

            var kumesHayvanLogs = await _kumesScadaDbContext.KumesHayvanLog
                .Where(x => x.KumesId == kumesId && x.DonemId == donem.Id && x.Silindi != true)
                .OrderBy(x => x.KayitZamani)
                .ToListAsync();

            int hayvanAdet = donem.HayvanAdet.GetValueOrDefault(0);
            int toplamOlenSayisi = kumesHayvanLogs
                .Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Olu)
                .Sum(x => x.Adet.GetValueOrDefault(0));

            int canliHayvanAdet = hayvanAdet - toplamOlenSayisi;
            if (canliHayvanAdet < 0) canliHayvanAdet = 0;

            // Cache'e 15 dakika boyunca koy
            _memoryCache.Set(cacheKey, canliHayvanAdet, TimeSpan.FromMinutes(15));

            return canliHayvanAdet;
        }

        public void ClearHayvanSayisiCache(int kumesId)
        {
            string cacheKey = $"GunlukHayvanSayisi_Kumes_{kumesId}";
            _memoryCache.Remove(cacheKey);
        }
        #endregion

        #region IcmeSuyu Tuketim Cache
        public async Task<decimal> GetDunIcmeSuyuSayacByKumesId(int kumesId, decimal icmeSuyuSayac)
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            string cacheKey = $"GunlukIcmeSuyuSayac_Kumes_{kumesId}_{today}";

            // Eğer cache varsa onu döndür
            if (_memoryCache.TryGetValue(cacheKey, out decimal cachedValue))
            {
                return cachedValue;
            }
            //eger dun aktif degilse lineer bolme islemi yaptirmaliyim
            // Su_Sayac
            var sonSuSayacAktifZaman = await _kumesScadaDbContext.EtiketLogYeni.Where(x => x.KumesId == kumesId && x.EtiketId == 25 && x.Deger == 1 && x.KayitZamani.Value.Date < DateTime.Today.Date).OrderByDescending(x => x.Id).Select(x => x.KayitZamani).FirstOrDefaultAsync();
            if (sonSuSayacAktifZaman == null)
            {
                return 0;
            }
            //else
            //{
            //    if (sonSuSayacAktifZaman.Value.Date==)
            //    {

            //    }
            //}

            var aktifSuSayacDegeri = await _kumesScadaDbContext.EtiketLogYeni
                .Where(x => x.KumesId == kumesId && x.EtiketId == 76 && x.KayitZamani.Value >= sonSuSayacAktifZaman.Value.AddSeconds(-10) && x.KayitZamani.Value <= sonSuSayacAktifZaman.Value.AddSeconds(10))
                .OrderByDescending(x => x.Id).FirstOrDefaultAsync();

            if (aktifSuSayacDegeri == null)
            {
                return 0;
            }

            int gunFarki = (DateTime.Now.Date - sonSuSayacAktifZaman.Value.Date).Days;
            if (gunFarki > 1)
            {
                decimal gunlukOrtalama = (icmeSuyuSayac - aktifSuSayacDegeri.Deger.GetValueOrDefault(0)) / gunFarki;
                var donulecekDeger = aktifSuSayacDegeri.Deger.GetValueOrDefault(0) + (gunlukOrtalama * (gunFarki - 1));
                _memoryCache.Set(cacheKey, donulecekDeger, TimeSpan.FromHours(1));
                return donulecekDeger;
            }
            else
            {
                _memoryCache.Set(cacheKey, aktifSuSayacDegeri.Deger.GetValueOrDefault(0m), TimeSpan.FromHours(1));
                return aktifSuSayacDegeri.Deger.GetValueOrDefault(0);
            }
        }

        public void ClearDunIcmeSuyuCache(int kumesId)
        {
            var yesterday = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
            string cacheKey = $"GunlukIcmeSuyuSayac_Kumes_{kumesId}_{yesterday}";
            _memoryCache.Remove(cacheKey);
        }
        #endregion

        #region Alarm Cache
        private string GetAlarmCacheKeyForKumes(int kumesId) => $"ActiveAlarms_Kumes_{kumesId.ToString()}";
        public List<AlarmLog> GetAlarmLogFromCache(int kumesId)
        {
            string cacheKey = GetAlarmCacheKeyForKumes(kumesId);
            _memoryCache.TryGetValue(cacheKey, out List<AlarmLog> alarmLogs);
            return alarmLogs;
        }

        public void SetKumesToAlarmCache(int kumesId, List<AlarmLog> alarmLogs)
        {
            string cacheKey = GetAlarmCacheKeyForKumes(kumesId);
            _memoryCache.Set(cacheKey, alarmLogs, TimeSpan.FromMinutes(10));
        }

        public async Task<List<AlarmLog>> GetAlarmLogsAsync(int kumesId/*, DateTime now*/)
        {
            var cachedAlarmLogs = new List<AlarmLog>();
            string cacheKey = GetAlarmCacheKeyForKumes(kumesId);

            // Cache'ten kontrol et
            var alarmLogs = GetAlarmLogFromCache(kumesId);
            if (alarmLogs != null)
            {
                return alarmLogs;
            }

            // Cache'te yoksa API'den çek ve ekle
            alarmLogs = await GetActiveAlarmsByKumesId(kumesId/*, now*/);
            if (alarmLogs != null)
            {
                SetKumesToAlarmCache(kumesId, alarmLogs);
            }

            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetActiveAlarmsByKumesId(int kumesId/*, DateTime now*/)
        {
            List<AlarmLog> activeAlarms = new List<AlarmLog>();
            activeAlarms = await _kumesScadaDbContext.AlarmLog.Where(x => x.KumesId == kumesId /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Aktif != false && x.Silindi != true).ToListAsync();

            return activeAlarms;
        }
        //public async Task SetOldAlarmsPassiveByKumesId(int kumesId, DateTime now)
        //{
        //    var oldActiveAlarms = await _kumesScadaDbContext.AlarmLog.Where(x => x.KumesId == kumesId && x.KayitZamani.Value.Date < now.Date && x.Aktif != false && x.Silindi != true).ToListAsync();
        //    var kumesIds = oldActiveAlarms.Select(x => x.KumesId.GetValueOrDefault(0)).Distinct().ToList();
        //    foreach (var oldAlarm in oldActiveAlarms)
        //    {
        //        oldAlarm.Aktif = false;
        //        oldAlarm.PasifZamani = now;
        //    }
        //    await _kumesScadaDbContext.SaveChangesAsync();
        //    ClearMemoryCache(kumesId);
        //}
        public async Task SetAlarmPassive(int kumesId, int alarmLogTipId/*, DateTime now*/)
        {
            //var oldActiveAlarms = await _kumesScadaDbContext.AlarmLog.Where(x => x.KayitZamani.Value.Date < now.Date && x.Aktif != false && x.Silindi != true).ToListAsync();
            //foreach (var oldAlarm in oldActiveAlarms)
            //{
            //    oldAlarm.Aktif = false;
            //    oldAlarm.PasifZamani = now;
            //}

            var activeAlarm = await _kumesScadaDbContext.AlarmLog.Where(x => x.KumesId == kumesId && x.AlarmEnum == alarmLogTipId/* && x.KayitZamani.Value.Date == now.Date*/ && x.Aktif != false && x.Silindi != true).FirstOrDefaultAsync();
            if (activeAlarm == null)
            {
                return;
            }
            if (activeAlarm != null)
            {
                activeAlarm.Aktif = false;
                activeAlarm.PasifZamani = DateTime.Now;
            }
            await _kumesScadaDbContext.SaveChangesAsync();
            ClearMemoryCache(kumesId);
        }

        public void ClearMemoryCache(int kumesId)
        {
            _memoryCache.Remove(GetAlarmCacheKeyForKumes(kumesId));
        }
        #endregion
    }
}