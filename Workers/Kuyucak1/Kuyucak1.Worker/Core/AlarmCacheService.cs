using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using static SharedKernel.ApplicationConstants;

namespace Kuyucak1.Worker.Core
{
    public class AlarmCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly TimeSpan CanliHayvanCacheSuresi = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DunSuSayacCacheSuresi = TimeSpan.FromHours(1);

        public AlarmCacheService(
            IMemoryCache memoryCache,
            IServiceScopeFactory scopeFactory)
        {
            _memoryCache = memoryCache;
            _scopeFactory = scopeFactory
                ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        #region KÃ¼mes / Hayvan

        public async Task<List<Kumes>> GetKumeslerAsync(
            int lokasyonId,
            CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            return await dbContext.Kumes
                .AsNoTracking()
                .Where(x =>
                    x.LokasyonId == lokasyonId &&
                    x.Silindi != true)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Kumes>> GetKumeslerByIdsAsync(
            IEnumerable<int> kumesIds,
            CancellationToken cancellationToken = default)
        {
            var ids = kumesIds.Distinct().ToArray();

            if (ids.Length == 0)
                return new List<Kumes>();

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<KumesScadaDbContext>();

            return await dbContext.Kumes
                .AsNoTracking()
                .Where(x =>
                    ids.Contains(x.Id) &&
                    x.Silindi != true)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);
        }


        /// <summary>
        /// BugÃ¼n gerÃ§ek bir Ã¶lÃ¼ hayvan kaydÄ± girilmiÅŸ mi?
        /// Silindi=true olan aylÄ±k placeholder kayÄ±tlarÄ± sayÄ±lmaz.
        /// Aktif dÃ¶nem yoksa alarm Ã¼retilmemesi iÃ§in true dÃ¶ner.
        /// </summary>
        public async Task<bool> GetOluHayvanSayisiGirildiMiByKumesId(
            int kumesId,
            CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var aktifDonemVar = await dbContext.Donem
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.KumesId == kumesId &&
                        x.Aktif != false &&
                        x.Silindi != true &&
                        x.CikisTarihi == null,
                    cancellationToken);

            if (!aktifDonemVar)
                return true;

            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            return await dbContext.KumesHayvanLog
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.KumesId == kumesId &&
                        x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                        x.Silindi != true &&
                        x.KayitZamani.HasValue &&
                        x.KayitZamani.Value >= bugun &&
                        x.KayitZamani.Value < yarin,
                    cancellationToken);
        }

        /// <summary>
        /// BugÃ¼n girilmiÅŸ gerÃ§ek Ã¶lÃ¼ hayvan sayÄ±sÄ±nÄ± dÃ¶ndÃ¼rÃ¼r.
        /// KayÄ±t henÃ¼z girilmemiÅŸse null dÃ¶ner; 0 girilmiÅŸse 0 dÃ¶ner.
        /// BÃ¶ylece "girilmedi" ile "0 Ã¶lÃ¼m" birbirinden ayrÄ±lÄ±r.
        /// </summary>
        public async Task<int?> GetGunlukOluHayvanSayisiByKumesId(
            int kumesId,
            CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            var kayitlar = await dbContext.KumesHayvanLog
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                    x.Silindi != true &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value >= bugun &&
                    x.KayitZamani.Value < yarin)
                .Select(x => x.Adet)
                .ToListAsync(cancellationToken);

            if (kayitlar.Count == 0)
                return null;

            return kayitlar.Sum(x => x.GetValueOrDefault(0));
        }

        /// <summary>
        /// En son tamamlanmÄ±ÅŸ hafta sonunun (Cumartesi + Pazar) her iki gÃ¼nÃ¼ iÃ§in
        /// gerÃ§ek Ã¶lÃ¼ hayvan kaydÄ± girilmiÅŸ mi?
        /// Silindi=true placeholder kayÄ±tlar sayÄ±lmaz.
        /// Aktif dÃ¶nem yoksa alarm Ã¼retilmemesi iÃ§in true dÃ¶ner.
        /// </summary>
        public async Task<bool> GetHaftaSonuOluHayvanSayisiGirildiMiByKumesId(
            int kumesId,
            CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var aktifDonemVar = await dbContext.Donem
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.KumesId == kumesId &&
                        x.Aktif != false &&
                        x.Silindi != true &&
                        x.CikisTarihi == null,
                    cancellationToken);

            if (!aktifDonemVar)
                return true;

            var bugun = DateTime.Today;

            // Pazartesi = 0 olacak ÅŸekilde haftanÄ±n baÅŸlangÄ±cÄ±nÄ± bul.
            var pazartesidenItibarenGun =
                ((int)bugun.DayOfWeek + 6) % 7;

            var buHaftaninPazartesisi =
                bugun.AddDays(-pazartesidenItibarenGun);

            var oncekiCumartesi =
                buHaftaninPazartesisi.AddDays(-2);

            var oncekiPazartesi =
                buHaftaninPazartesisi;

            var girilenGunler = await dbContext.KumesHayvanLog
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                    x.Silindi != true &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value >= oncekiCumartesi &&
                    x.KayitZamani.Value < oncekiPazartesi)
                .Select(x => x.KayitZamani!.Value.Date)
                .Distinct()
                .ToListAsync(cancellationToken);

            return girilenGunler.Count >= 2;
        }

        /// <summary>
        /// Aktif dÃ¶nemin baÅŸlangÄ±Ã§ hayvan sayÄ±sÄ±ndan gerÃ§ek Ã¶lÃ¼m kayÄ±tlarÄ±nÄ± dÃ¼ÅŸer.
        /// Bellekte sadece int deÄŸer tutulur ve 15 dakika cache'lenir.
        /// </summary>
        public async Task<int> GetGunlukCanliHayvanSayisiByKumesId(
            int kumesId,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"GunlukHayvanSayisi_Kumes_{kumesId}";

            if (_memoryCache.TryGetValue(cacheKey, out int cachedValue))
                return cachedValue;

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var donem = await dbContext.Donem
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.Aktif != false &&
                    x.Silindi != true &&
                    x.CikisTarihi == null)
                .Select(x => new
                {
                    x.Id,
                    HayvanAdet = x.HayvanAdet ?? 0
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (donem == null)
                return 0;

            /*
             * BÃ¼tÃ¼n KumesHayvanLog kayÄ±tlarÄ±nÄ± belleÄŸe almak yerine SUM SQL'de yapÄ±lÄ±r.
             * Silindi=true aylÄ±k placeholder kayÄ±tlarÄ± hesaba katÄ±lmaz.
             */
            var toplamOlenSayisi = await dbContext.KumesHayvanLog
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.DonemId == donem.Id &&
                    x.Silindi != true &&
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu)
                .SumAsync(
                    x => (int?)x.Adet,
                    cancellationToken)
                ?? 0;

            var canliHayvanAdet = donem.HayvanAdet - toplamOlenSayisi;

            if (canliHayvanAdet < 0)
                canliHayvanAdet = 0;

            _memoryCache.Set(
                cacheKey,
                canliHayvanAdet,
                CanliHayvanCacheSuresi);

            return canliHayvanAdet;
        }

        public void ClearHayvanSayisiCache(int kumesId)
        {
            _memoryCache.Remove($"GunlukHayvanSayisi_Kumes_{kumesId}");
        }

        #endregion

        #region Ä°Ã§me Suyu TÃ¼ketim Cache

        /// <summary>
        /// DÃ¼nkÃ¼/son geÃ§erli iÃ§me suyu sayaÃ§ referansÄ±nÄ± bulur.
        /// EtiketId'ler worker'Ä±n EslemeSozcugu cache'inden gelir; burada statik ID yoktur.
        /// </summary>
        public async Task<decimal> GetDunIcmeSuyuSayacByKumesId(
            int kumesId,
            decimal icmeSuyuSayac,
            int suSayacAktifEtiketId,
            int suSayacEtiketId,
            CancellationToken cancellationToken = default)
        {
            var bugunAnahtari = DateTime.Today.ToString("yyyyMMdd");
            var cacheKey = $"GunlukIcmeSuyuSayac_Kumes_{kumesId}_{bugunAnahtari}";

            if (_memoryCache.TryGetValue(cacheKey, out decimal cachedValue))
                return cachedValue;

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var bugun = DateTime.Today;

            var sonSuSayacAktifZaman = await dbContext.EtiketLogYeni
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.EtiketId == suSayacAktifEtiketId &&
                    x.Deger == 1 &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value < bugun)
                .OrderByDescending(x => x.KayitZamani)
                .Select(x => x.KayitZamani)
                .FirstOrDefaultAsync(cancellationToken);

            if (!sonSuSayacAktifZaman.HasValue)
                return 0;

            var aralikBaslangic =
                sonSuSayacAktifZaman.Value.AddSeconds(-10);

            var aralikBitis =
                sonSuSayacAktifZaman.Value.AddSeconds(10);

            var aktifSuSayacDegeri = await dbContext.EtiketLogYeni
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.EtiketId == suSayacEtiketId &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value >= aralikBaslangic &&
                    x.KayitZamani.Value <= aralikBitis)
                .OrderByDescending(x => x.KayitZamani)
                .Select(x => x.Deger)
                .FirstOrDefaultAsync(cancellationToken);

            if (!aktifSuSayacDegeri.HasValue)
                return 0;

            var gunFarki =
                (DateTime.Today - sonSuSayacAktifZaman.Value.Date).Days;

            decimal donulecekDeger;

            if (gunFarki > 1)
            {
                var gunlukOrtalama =
                    (icmeSuyuSayac - aktifSuSayacDegeri.Value) /
                    gunFarki;

                donulecekDeger =
                    aktifSuSayacDegeri.Value +
                    (gunlukOrtalama * (gunFarki - 1));
            }
            else
            {
                donulecekDeger = aktifSuSayacDegeri.Value;
            }

            _memoryCache.Set(
                cacheKey,
                donulecekDeger,
                DunSuSayacCacheSuresi);

            return donulecekDeger;
        }

        /*
         * Eski Ã§aÄŸrÄ±larÄ± kÄ±rmamak iÃ§in bÄ±rakÄ±ldÄ±.
         * Yeni KayÄ±ÅŸlar worker bu overload'u kullanmaz.
         */
        public Task<decimal> GetDunIcmeSuyuSayacByKumesId(
            int kumesId,
            decimal icmeSuyuSayac)
        {
            return GetDunIcmeSuyuSayacByKumesId(
                kumesId,
                icmeSuyuSayac,
                25,
                76,
                CancellationToken.None);
        }

        public void ClearDunIcmeSuyuCache(int kumesId)
        {
            var bugun = DateTime.Today.ToString("yyyyMMdd");
            _memoryCache.Remove($"GunlukIcmeSuyuSayac_Kumes_{kumesId}_{bugun}");
        }

        #endregion

        #region Alarm Cache

        private string GetAlarmCacheKeyForKumes(int kumesId) =>
            $"ActiveAlarms_Kumes_{kumesId}";

        public List<AlarmLog>? GetAlarmLogFromCache(int kumesId)
        {
            var cacheKey = GetAlarmCacheKeyForKumes(kumesId);
            _memoryCache.TryGetValue(cacheKey, out List<AlarmLog>? alarmLogs);
            return alarmLogs;
        }

        public void SetKumesToAlarmCache(
            int kumesId,
            List<AlarmLog> alarmLogs)
        {
            _memoryCache.Set(
                GetAlarmCacheKeyForKumes(kumesId),
                alarmLogs,
                TimeSpan.FromMinutes(10));
        }

        public async Task<List<AlarmLog>> GetAlarmLogsAsync(
            int kumesId,
            CancellationToken cancellationToken = default)
        {
            var alarmLogs = GetAlarmLogFromCache(kumesId);

            if (alarmLogs != null)
                return alarmLogs;

            alarmLogs = await GetActiveAlarmsByKumesId(
                kumesId,
                cancellationToken);

            SetKumesToAlarmCache(kumesId, alarmLogs);

            return alarmLogs;
        }

        public async Task<List<AlarmLog>> GetActiveAlarmsByKumesId(
            int kumesId,
            CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            return await dbContext.AlarmLog
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.Aktif != false &&
                    x.Silindi != true)
                .ToListAsync(cancellationToken);
        }

        public async Task SetAlarmPassive(
            int kumesId,
            int alarmLogTipId,
            CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            var activeAlarm = await dbContext.AlarmLog
                .FirstOrDefaultAsync(
                    x =>
                        x.KumesId == kumesId &&
                        x.AlarmEnum == alarmLogTipId &&
                        x.Aktif != false &&
                        x.Silindi != true,
                    cancellationToken);

            if (activeAlarm == null)
                return;

            activeAlarm.Aktif = false;
            activeAlarm.PasifZamani = DateTime.Now;

            await dbContext.SaveChangesAsync(cancellationToken);

            ClearMemoryCache(kumesId);
        }

        public void ClearMemoryCache(int kumesId)
        {
            _memoryCache.Remove(GetAlarmCacheKeyForKumes(kumesId));
        }

        #endregion
    }
}
