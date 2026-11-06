using KumesScada.Domain.Models.ReportModels;
using Microsoft.Extensions.Caching.Memory;
using SharedKernel;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace EnerjiIzleme.Application.Services.Core
{
    public interface IHayvanSayisiCacheMemory
    {
        Task<Dictionary<int, Dictionary<DateTime, long>>> GetRangeAsync(IEnumerable<int> nodeIds, DateTime startDate, DateTime endDate, CancellationToken ct);
    }
    public sealed class HayvanSayisiCacheMemory : IHayvanSayisiCacheMemory
    {
        private readonly IMemoryCache _cache;
        private readonly HttpClient _http;

        // aynı key için aynı anda 50 request gitmesin
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public HayvanSayisiCacheMemory(HttpClient http, IMemoryCache cache)
        {
            _cache = cache;
            _http = http;
        }
        
        public async Task<List<AnimalCountRangeItem>> GetAnimalCountsRangeAsync(AnimalCountRangeRequest request, CancellationToken ct)
        {
            var requestUrl = new Uri($"{ApiUrls.KumesScadaBaseUrl}/Kumes/range");
            using var resp = await _http.PostAsJsonAsync(requestUrl, request, ct);
            resp.EnsureSuccessStatusCode();

            var data = await resp.Content.ReadFromJsonAsync<List<AnimalCountRangeItem>>(cancellationToken: ct);
            return data ?? new List<AnimalCountRangeItem>();
        }

        public async Task<Dictionary<int, Dictionary<DateTime, long>>> GetRangeAsync(IEnumerable<int> nodeIds, DateTime startDate, DateTime endDate, CancellationToken ct)
        {
            var ids = nodeIds.Distinct().OrderBy(x => x).ToArray();
            var key = BuildKey(ids, startDate, endDate);

            if (_cache.TryGetValue(key, out Dictionary<int, Dictionary<DateTime, long>>? cached) && cached != null)
                return cached;

            var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(ct);
            try
            {
                // lock içinde tekrar kontrol (double-check)
                if (_cache.TryGetValue(key, out cached) && cached != null)
                    return cached;

                var resp = await GetAnimalCountsRangeAsync(new AnimalCountRangeRequest
                {
                    NodeIds = ids,
                    StartDate = startDate,
                    EndDate = endDate
                }, ct);

                // Dönüşü hızlı bir map’e çevir
                var map = new Dictionary<int, Dictionary<DateTime, long>>(ids.Length);
                foreach (var id in ids) map[id] = new Dictionary<DateTime, long>();

                foreach (var item in resp)
                {
                    if (!map.TryGetValue(item.NodeId, out var dayMap))
                    {
                        dayMap = new Dictionary<DateTime, long>();
                        map[item.NodeId] = dayMap;
                    }
                    dayMap[item.Date] = item.Count;
                }

                // Cache TTL: hayvan sayısı sık değişmez → 10-30 dk iyi
                _cache.Set(key, map, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
                });

                return map;
            }
            finally
            {
                sem.Release();
            }
        }

        private static string BuildKey(int[] ids, DateTime start, DateTime end)
            => $"animalcount:{start:yyyyMMdd}:{end:yyyyMMdd}:{string.Join(",", ids)}";
    }
}
