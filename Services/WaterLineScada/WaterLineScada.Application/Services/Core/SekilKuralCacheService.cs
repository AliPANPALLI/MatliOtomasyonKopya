using Microsoft.Extensions.Caching.Memory;
using WaterLineScada.Domain.Interfaces;
using WaterLineScada.Domain.Models;

namespace WaterLineScada.Application.Services.Core
{
    public class SekilKuralCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ICizimRepository _cizimRepository;
        public SekilKuralCacheService(IMemoryCache memoryCache, ICizimRepository cizimRepository)
        {
            _memoryCache = memoryCache;
            _cizimRepository = cizimRepository;
        }
        //etiketler,sekiller,cizimsekileslemeler
        public async Task<List<CizimSekilEsleme>> GetCizimSekilEslemelerAsync(int cizimId)
        {
            var cacheKey = $"cizim-sekil-eslemeler-{cizimId}";

            if (!_memoryCache.TryGetValue(cacheKey, out List<CizimSekilEsleme> result))
            {
                result = await _cizimRepository.GetCizimSekilEslemesByCizimId(cizimId);
                _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            }

            return result;
        }

        public async Task<List<Sekil>> GetSekillerAsync(List<int> sekilIds)
        {
            var key = "sekil-list-" + string.Join(",", sekilIds.OrderBy(x => x));
            if (!_memoryCache.TryGetValue(key, out List<Sekil> result))
            {
                result = await _cizimRepository.GetCizimEtiketler(sekilIds);
                _memoryCache.Set(key, result, TimeSpan.FromMinutes(30));
            }

            return result;
        }

        public async Task<List<SekilEtiketKural>> GetEtiketKurallarAsync(List<int> sekilIds)
        {
            var key = "kural-list-" + string.Join(",", sekilIds.OrderBy(x => x));
            if (!_memoryCache.TryGetValue(key, out List<SekilEtiketKural> result))
            {
                result = await _cizimRepository.GetEtiketKural(sekilIds);
                _memoryCache.Set(key, result, TimeSpan.FromMinutes(30));
            }

            return result;
        }

        public void ClearCizimCache(int cizimId)
        {
            _memoryCache.Remove($"cizim-sekil-eslemeler-{cizimId}");
        }

        public void ClearSekilCache(List<int> sekilIds)
        {
            var key = "sekil-list-" + string.Join(",", sekilIds.OrderBy(x => x));
            _memoryCache.Remove(key);
        }

        public void ClearKuralCache(List<int> sekilIds)
        {
            var key = "kural-list-" + string.Join(",", sekilIds.OrderBy(x => x));
            _memoryCache.Remove(key);
        }

    }
}