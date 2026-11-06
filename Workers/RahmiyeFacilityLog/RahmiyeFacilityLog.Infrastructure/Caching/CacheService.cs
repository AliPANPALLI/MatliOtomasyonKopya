using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static SharedKernel.ApplicationConstants;

namespace RahmiyeFacilityLog.Infrastructure.Caching
{
    public class CacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly KumesScadaDbContext _kumesScadaDbContext;

        public CacheService(IMemoryCache memoryCache, KumesScadaDbContext kumesScadaDbContext)
        {
            _memoryCache = memoryCache;
            _kumesScadaDbContext = kumesScadaDbContext ?? throw new ArgumentNullException(nameof(kumesScadaDbContext));
        }

        public async Task<List<Kumes>> GetKumeslerAsync(int lokasyonId)
        {
            var kumesler = await _kumesScadaDbContext.Kumes.Where(x => x.LokasyonId == lokasyonId /*&& x.Aktif != false*/ && x.Silindi != true).ToListAsync();
            return kumesler;
        }

        //public async Task<List<EtiketLogYeni>> GetEtiketLogYeniByKumesId(int kumesId)
        //{
        //    try
        //    {
        //        List<int> yumurtaEtiketIds = new List<int> { 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124 };
        //        var etiketLogs = await _kumesScadaDbContext.EtiketLogYeni
        //            .Where(x => x.KumesId == 1 && yumurtaEtiketIds.Contains(x.EtiketId.GetValueOrDefault(0)) && x.KayitZamani.Value >= new DateTime(2026, 2, 6, 16, 35, 0) && x.KayitZamani.Value <= new DateTime(2026, 2, 7, 12, 27, 0)).AsNoTracking().ToListAsync();
        //        return etiketLogs;
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}

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
    }
}
