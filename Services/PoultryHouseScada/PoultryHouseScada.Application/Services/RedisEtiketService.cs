using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Domain.Models.ReportModels;
using StackExchange.Redis;
using System.Text.Json;

namespace PoultryHouseScada.Application.Services
{
    public class RedisEtiketService
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _ttl = TimeSpan.FromHours(24); // İstediğiniz süre

        public RedisEtiketService(string connectionString)
        {
            var redis = ConnectionMultiplexer.Connect(connectionString);
            _db = redis.GetDatabase();
        }

        // Tüm listeyi Redis'e bas
        public async Task BulkSetEtiketLogAsync(IEnumerable<EtiketLog_TempYeni> etiketLogs)
        {
            // KumesId'ye göre grupla, her kumes için tek HSET işlemi yap
            var grouped = etiketLogs
                .Where(x => x.KumesId.HasValue && x.EtiketId.HasValue)
                .GroupBy(x => x.KumesId!.Value);

            foreach (var group in grouped)
            {
                var key = $"kumes:{group.Key}:etiketler";

                var hashFields = group.Select(e => new HashEntry(
                    e.EtiketId!.Value.ToString(),
                    e.Deger.HasValue ? e.Deger.Value.ToString() : "0"
                )).ToArray();

                await _db.HashSetAsync(key, hashFields);
                await _db.KeyExpireAsync(key, _ttl);
            }
        }

        // Tek etiket değeri al (API'de kullanım için)
        public async Task<decimal?> GetEtiketDegerAsync(int kumesId, int etiketId)
        {
            var key = $"kumes:{kumesId}:etiketler";
            var value = await _db.HashGetAsync(key, etiketId.ToString());

            if (value.IsNullOrEmpty) return null;
            if (decimal.TryParse(value.ToString(), out var result)) return result;
            return null;
        }

        // Bir kümesin tüm etiketlerini al
        public async Task<Dictionary<int, decimal?>> GetAllEtiketlerAsync(int kumesId)
        {
            var key = $"kumes:{kumesId}:etiketler";
            var entries = await _db.HashGetAllAsync(key);

            return entries.ToDictionary(
                e => int.Parse(e.Name!),
                e => decimal.TryParse(e.Value.ToString(), out var val) ? (decimal?)val : null
            );
        }

        public async Task<List<EtiketLog_TempYeni>> GetEtiketLogVerilerByKumesId(int kumesId)
        {
            var key = $"kumes:{kumesId}:etiketler";
            var entries = await _db.HashGetAllAsync(key);

            if (!entries.Any()) return new List<EtiketLog_TempYeni>();

            var besDakikaOnce = DateTime.Now.AddMinutes(-5);

            return entries
                .Select(e =>
                {
                    var parsed = JsonSerializer.Deserialize<EtiketRedisValue>(e.Value.ToString());
                    return new EtiketLog_TempYeni
                    {
                        KumesId = kumesId,
                        EtiketId = int.Parse(e.Name!),
                        Deger = parsed?.Deger,
                        KayitZamani = parsed?.KayitZamani
                    };
                })
                .Where(x => x.KayitZamani.HasValue && x.KayitZamani.Value >= besDakikaOnce).OrderByDescending(x => x.EtiketId).ToList();
        }

        //public async Task<List<EtiketLog_TempYeni>> GetEtiketLogVerilerByKumesId(int kumesId)
        //{
        //    var key = $"kumes:{kumesId}:etiketler";
        //    var entries = await _db.HashGetAllAsync(key);

        //    if (!entries.Any()) return new List<EtiketLog_TempYeni>();

        //    return entries
        //        .Select(e =>
        //        {
        //            var parsed = JsonSerializer.Deserialize<EtiketRedisValue>(e.Value.ToString());
        //            return new EtiketLog_TempYeni
        //            {
        //                KumesId = kumesId,
        //                EtiketId = int.Parse(e.Name!),
        //                Deger = parsed?.Deger,
        //                KayitZamani = parsed?.KayitZamani
        //            };
        //        })
        //        .OrderByDescending(x => x.EtiketId)
        //        .ToList();
        //}
    }
}