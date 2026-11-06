using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using static SharedKernel.ApplicationConstants;

namespace Kuyucak1LogWorker.Core
{
    public class CacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IServiceScopeFactory _scopeFactory;

        public CacheService(
            IMemoryCache memoryCache,
            IServiceScopeFactory scopeFactory)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        public async Task<List<Kumes>> GetKumeslerAsync(
            int lokasyonId,
            CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

            return await dbContext.Kumes
                .AsNoTracking()
                .Where(x =>
                    x.LokasyonId == lokasyonId &&
                    x.Silindi != true)
                .ToListAsync(cancellationToken);
        }

        #region Hayvan Sayısı Cache

        public async Task<int> GetGunlukCanliHayvanSayisiByKumesId(
            int kumesId,
            CancellationToken cancellationToken)
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
                .FirstOrDefaultAsync(cancellationToken);

            if (donem == null)
            {
                return 0;
            }

            var toplamOlenSayisi = await dbContext.KumesHayvanLog
                .AsNoTracking()
                .Where(x =>
                    x.KumesId == kumesId &&
                    x.DonemId == donem.Id &&
                    x.Silindi != true &&
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu)
                .SumAsync(x => (int?)x.Adet, cancellationToken) ?? 0;

            int hayvanAdet = donem.HayvanAdet.GetValueOrDefault(0);
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
            decimal icmeSuyuSayac,
            int suSayacAktifEtiketId,
            int suSayacEtiketId,
            CancellationToken cancellationToken = default)
        {
            var bugun = DateTime.Today;
            var cacheKey = $"GunlukIcmeSuyuSayac_Kumes_{kumesId}_{bugun:yyyyMMdd}";

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
                    x.EtiketId == suSayacAktifEtiketId &&
                    x.Deger == 1 &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value < bugun)
                .OrderByDescending(x => x.KayitZamani)
                .Select(x => x.KayitZamani)
                .FirstOrDefaultAsync(cancellationToken);

            if (!sonSuSayacAktifZaman.HasValue)
            {
                return 0;
            }

            var aralikBaslangic = sonSuSayacAktifZaman.Value.AddSeconds(-10);
            var aralikBitis = sonSuSayacAktifZaman.Value.AddSeconds(10);

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
            {
                return 0;
            }

            var gunFarki = (bugun - sonSuSayacAktifZaman.Value.Date).Days;
            decimal donulecekDeger;

            if (gunFarki > 1)
            {
                var fark = icmeSuyuSayac - aktifSuSayacDegeri.Value;

                if (fark < 0)
                {
                    donulecekDeger = aktifSuSayacDegeri.Value;
                }
                else
                {
                    var gunlukOrtalama = fark / gunFarki;
                    donulecekDeger =
                        aktifSuSayacDegeri.Value +
                        (gunlukOrtalama * (gunFarki - 1));
                }
            }
            else
            {
                donulecekDeger = aktifSuSayacDegeri.Value;
            }

            _memoryCache.Set(
                cacheKey,
                donulecekDeger,
                TimeSpan.FromHours(1));

            return donulecekDeger;
        }

        #endregion

        public async Task<int?> GetGunlukOluHayvanSayisiByKumesId(
            int kumesId,
            CancellationToken cancellationToken = default)
        {
            var bugun = DateTime.Today;
            var yarin = bugun.AddDays(1);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();

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

            // null = bugün kullanıcı henüz kayıt girmedi.
            // 0    = kayıt girildi fakat ölüm yok.
            if (kayitlar.Count == 0)
            {
                return null;
            }

            return kayitlar.Sum(x => x.GetValueOrDefault(0));
        }
    }
}
