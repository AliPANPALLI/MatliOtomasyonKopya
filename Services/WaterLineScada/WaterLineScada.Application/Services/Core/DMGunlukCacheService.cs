using Microsoft.Extensions.Caching.Memory;
using WaterLineScada.Domain.Interfaces;
using WaterLineScada.Domain.Models;

namespace WaterLineScada.Application.Services.Core
{
    public class DMGunlukCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IEtiketLogRepository _etiketLogRepository;
        public DMGunlukCacheService(IMemoryCache memoryCache, IEtiketLogRepository etiketLogRepository)
        {
            _memoryCache = memoryCache;
            _etiketLogRepository = etiketLogRepository;
        }

        private string GetCacheKeyForEtiketId(int etiketId) => $"DMGunlukCache_{etiketId}";
        private string GetMissingCacheKeyForEtiketId(int etiketId) => $"DMGunlukCache_Missing_{etiketId}";

        public async Task<EtiketLogYeni?> GetGunSonuVerilerForEtiketId(int etiketId)
        {
            var values = await GetGunSonuVerilerForEtiketIds(new[] { etiketId });
            return values.FirstOrDefault();
        }

        public async Task<List<EtiketLogYeni>> GetGunSonuVerilerForEtiketIds(IEnumerable<int> etiketIds)
        {
            var ids = etiketIds.Distinct().ToList();
            var result = new List<EtiketLogYeni>(ids.Count);
            var missingIds = new List<int>();
            var yesterday = DateTime.Today.AddDays(-1);

            foreach (var etiketId in ids)
            {
                var cachedValue = GetGunSonuVeriForEtiketIdFromCache(etiketId);
                if (cachedValue?.KayitZamani?.Date == yesterday)
                {
                    result.Add(cachedValue);
                    continue;
                }

                if (cachedValue != null)
                {
                    _memoryCache.Remove(GetCacheKeyForEtiketId(etiketId));
                }

                if (_memoryCache.TryGetValue(
                        GetMissingCacheKeyForEtiketId(etiketId),
                        out DateTime missingForDay) &&
                    missingForDay == yesterday)
                {
                    continue;
                }

                missingIds.Add(etiketId);
            }

            if (missingIds.Count == 0)
            {
                return result;
            }

            var loadedValues = await _etiketLogRepository.GetEtiketLogGunSonuVeriler(missingIds);
            foreach (var loadedValue in loadedValues)
            {
                if (!loadedValue.EtiketId.HasValue)
                {
                    continue;
                }

                _memoryCache.Remove(GetMissingCacheKeyForEtiketId(loadedValue.EtiketId.Value));
                SetGunSonuVeriToCache(loadedValue.EtiketId.Value, loadedValue);
                result.Add(loadedValue);
            }

            var loadedEtiketIds = loadedValues
                .Where(x => x.EtiketId.HasValue)
                .Select(x => x.EtiketId!.Value)
                .ToHashSet();
            foreach (var missingId in missingIds.Where(x => !loadedEtiketIds.Contains(x)))
            {
                _memoryCache.Set(
                    GetMissingCacheKeyForEtiketId(missingId),
                    yesterday,
                    TimeSpan.FromMinutes(5));
            }

            return result;
        }

        public void SetGunSonuVeriToCache(int etiketId, EtiketLogYeni gunSonuVeri)
        {
            string cacheKey = GetCacheKeyForEtiketId(etiketId);
            _memoryCache.Set(cacheKey, gunSonuVeri, TimeSpan.FromMinutes(180));
        }

        public EtiketLogYeni? GetGunSonuVeriForEtiketIdFromCache(int etiketId)
        {
            string cacheKey = GetCacheKeyForEtiketId(etiketId);
            _memoryCache.TryGetValue(cacheKey, out EtiketLogYeni? gunSonuVeri);
            return gunSonuVeri;
        }
    }
}
