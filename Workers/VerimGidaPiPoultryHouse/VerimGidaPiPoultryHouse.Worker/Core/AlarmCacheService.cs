using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using static SharedKernel.ApplicationConstants;

namespace VerimGidaPiPoultryHouse.Worker.Core
{
    public class AlarmCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceScopeFactory _scopeFactory;

        public AlarmCacheService(
            IMemoryCache memoryCache,
            IServiceScopeFactory scopeFactory)
        {
            _memoryCache = memoryCache;
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> GetOluHayvanSayisiGirildiMiByKumesId(int kumesId)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var donem = await dbContext.Donem
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.Aktif != false &&
                    x.Silindi != true &&
                    x.CikisTarihi == null)
                .FirstOrDefaultAsync();

            if (donem == null)
            {
                return true;
            }

            var oluHayvanSayisiGirildiMi = await dbContext.KumesHayvanLog
                .AnyAsync(x =>
                    x.KumesId == kumesId &&
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value.Date == DateTime.Now.Date &&
                    x.Silindi != true);

            return oluHayvanSayisiGirildiMi;
        }

        public async Task<bool> GetHaftaSonuOluHayvanSayisiGirildiMiByKumesId(int kumesId)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var donem = await dbContext.Donem
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.Aktif != false &&
                    x.Silindi != true &&
                    x.CikisTarihi == null)
                .FirstOrDefaultAsync();

            if (donem == null)
            {
                return true;
            }

            var baslangicTarihi = DateTime.Now.AddDays(-2).Date;
            var bitisTarihi = DateTime.Now.Date;

            var kayitSayisi = await dbContext.KumesHayvanLog
                .CountAsync(x =>
                    x.KumesId == kumesId &&
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value.Date >= baslangicTarihi &&
                    x.KayitZamani.Value.Date < bitisTarihi &&
                    x.Silindi != true);

            return kayitSayisi >= 2;
        }

        public async Task<List<Kumes>> GetKumeslerAsync(int lokasyonId)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            return await dbContext.Kumes
                .AsNoTracking()
                .Where(x =>
                    x.LokasyonId == lokasyonId &&
                    x.Silindi != true)
                .ToListAsync();
        }

        #region Hayvan Sayısı Cache

        public async Task<int> GetGunlukCanliHayvanSayisiByKumesId(int kumesId)
        {
            string cacheKey = $"GunlukHayvanSayisi_Kumes_{kumesId}";

            if (_memoryCache.TryGetValue(cacheKey, out int cachedValue))
            {
                return cachedValue;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var donem = await dbContext.Donem
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.Aktif != false &&
                    x.Silindi != true &&
                    x.CikisTarihi == null)
                .FirstOrDefaultAsync();

            if (donem == null)
            {
                return 0;
            }

            var kumesHayvanLogs = await dbContext.KumesHayvanLog
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.DonemId == donem.Id &&
                    x.Silindi != true)
                .ToListAsync();

            int hayvanAdet = donem.HayvanAdet.GetValueOrDefault(0);

            int toplamOlenSayisi = kumesHayvanLogs
                .Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Olu)
                .Sum(x => x.Adet.GetValueOrDefault(0));

            int canliHayvanAdet = hayvanAdet - toplamOlenSayisi;

            if (canliHayvanAdet < 0)
            {
                canliHayvanAdet = 0;
            }

            _memoryCache.Set(
                cacheKey,
                canliHayvanAdet,
                TimeSpan.FromMinutes(15));

            return canliHayvanAdet;
        }

        public void ClearHayvanSayisiCache(int kumesId)
        {
            string cacheKey = $"GunlukHayvanSayisi_Kumes_{kumesId}";
            _memoryCache.Remove(cacheKey);
        }

        #endregion

        #region IcmeSuyu Tuketim Cache

        public async Task<decimal> GetDunIcmeSuyuSayacByKumesId(
            int kumesId,
            decimal icmeSuyuSayac)
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            string cacheKey = $"GunlukIcmeSuyuSayac_Kumes_{kumesId}_{today}";

            if (_memoryCache.TryGetValue(cacheKey, out decimal cachedValue))
            {
                return cachedValue;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var sonSuSayacAktifZaman = await dbContext.EtiketLogYeni
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.EtiketId == 25 &&
                    x.Deger == 1 &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value.Date < DateTime.Today)
                .OrderByDescending(x => x.Id)
                .Select(x => x.KayitZamani)
                .FirstOrDefaultAsync();

            if (sonSuSayacAktifZaman == null)
            {
                return 0;
            }

            var aktifSuSayacDegeri = await dbContext.EtiketLogYeni
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.EtiketId == 76 &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value >= sonSuSayacAktifZaman.Value.AddSeconds(-10) &&
                    x.KayitZamani.Value <= sonSuSayacAktifZaman.Value.AddSeconds(10))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (aktifSuSayacDegeri == null)
            {
                return 0;
            }

            int gunFarki =
                (DateTime.Now.Date - sonSuSayacAktifZaman.Value.Date).Days;

            if (gunFarki > 1)
            {
                decimal gunlukOrtalama =
                    (icmeSuyuSayac -
                     aktifSuSayacDegeri.Deger.GetValueOrDefault(0)) /
                    gunFarki;

                var donulecekDeger =
                    aktifSuSayacDegeri.Deger.GetValueOrDefault(0) +
                    (gunlukOrtalama * (gunFarki - 1));

                _memoryCache.Set(
                    cacheKey,
                    donulecekDeger,
                    TimeSpan.FromHours(1));

                return donulecekDeger;
            }

            var oncekiSayacDegeri =
                aktifSuSayacDegeri.Deger.GetValueOrDefault(0m);

            _memoryCache.Set(
                cacheKey,
                oncekiSayacDegeri,
                TimeSpan.FromHours(1));

            return oncekiSayacDegeri;
        }

        public void ClearDunIcmeSuyuCache(int kumesId)
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            string cacheKey = $"GunlukIcmeSuyuSayac_Kumes_{kumesId}_{today}";
            _memoryCache.Remove(cacheKey);
        }

        #endregion

        #region Alarm Cache

        private string GetAlarmCacheKeyForKumes(int kumesId)
        {
            return $"ActiveAlarms_Kumes_{kumesId}";
        }

        public List<AlarmLog>? GetAlarmLogFromCache(int kumesId)
        {
            string cacheKey = GetAlarmCacheKeyForKumes(kumesId);
            _memoryCache.TryGetValue(cacheKey, out List<AlarmLog>? alarmLogs);
            return alarmLogs;
        }

        public void SetKumesToAlarmCache(
            int kumesId,
            List<AlarmLog> alarmLogs)
        {
            string cacheKey = GetAlarmCacheKeyForKumes(kumesId);

            _memoryCache.Set(
                cacheKey,
                alarmLogs,
                TimeSpan.FromMinutes(10));
        }

        public async Task<List<AlarmLog>> GetAlarmLogsAsync(int kumesId)
        {
            var alarmLogs = GetAlarmLogFromCache(kumesId);

            if (alarmLogs != null)
            {
                return alarmLogs;
            }

            alarmLogs = await GetActiveAlarmsByKumesId(kumesId);
            SetKumesToAlarmCache(kumesId, alarmLogs);

            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetActiveAlarmsByKumesId(int kumesId)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            return await dbContext.AlarmLog
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.Aktif != false &&
                    x.Silindi != true)
                .ToListAsync();
        }

        public async Task SetAlarmPassive(
            int kumesId,
            int alarmLogTipId)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var activeAlarm = await dbContext.AlarmLog
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.AlarmEnum == alarmLogTipId &&
                    x.Aktif != false &&
                    x.Silindi != true)
                .FirstOrDefaultAsync();

            if (activeAlarm == null)
            {
                return;
            }

            activeAlarm.Aktif = false;
            activeAlarm.PasifZamani = DateTime.Now;

            await dbContext.SaveChangesAsync();
            ClearMemoryCache(kumesId);
        }

        public void ClearMemoryCache(int kumesId)
        {
            _memoryCache.Remove(GetAlarmCacheKeyForKumes(kumesId));
        }

        #endregion
    }
}
