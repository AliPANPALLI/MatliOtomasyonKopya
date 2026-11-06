using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
namespace Authentication.API.Security;

// Process-local pending challenges expire on restart. Multi-instance deployments need a shared atomic store.
public sealed class AuthChallenges(IMemoryCache cache)
{
    private readonly object gate = new();
    private sealed record Entry(string Purpose, string UserId, string Stamp, byte[] CodeHash, DateTimeOffset Expires, string LatestKey)
    { public int Attempts { get; set; } }
    private sealed class Counter { public int Value; }
    public static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string NewId() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public string Issue(string purpose, string userId, string passwordHash, string code, TimeSpan lifetime)
    {
        lock (gate)
        {
            var id = NewId();
            var latestKey = $"auth-latest:{purpose}:{userId}";
            if (cache.TryGetValue<string>(latestKey, out var previous)) cache.Remove("auth:" + previous);
            var entry = new Entry(purpose, userId, Fingerprint(passwordHash), SHA256.HashData(Encoding.UTF8.GetBytes(code)), DateTimeOffset.UtcNow.Add(lifetime), latestKey);
            cache.Set("auth:" + id, entry, entry.Expires);
            cache.Set(latestKey, id, entry.Expires);
            return id;
        }
    }
    public bool TryConsume(string purpose, string? id, string? code, out string userId, out string stamp)
    {
        userId = stamp = "";
        if (string.IsNullOrEmpty(id) || id.Length != 64 || string.IsNullOrEmpty(code) || code.Length > 16) return false;
        lock (gate)
        {
            if (!cache.TryGetValue<Entry>("auth:" + id, out var entry) || entry is null || entry.Purpose != purpose || entry.Expires <= DateTimeOffset.UtcNow) return false;
            entry.Attempts++;
            var matches = CryptographicOperations.FixedTimeEquals(entry.CodeHash, SHA256.HashData(Encoding.UTF8.GetBytes(code)));
            if (matches || entry.Attempts >= 3) { cache.Remove("auth:" + id); cache.Remove(entry.LatestKey); }
            if (!matches) return false;
            userId = entry.UserId; stamp = entry.Stamp;
            return true;
        }
    }
    public bool Allow(string action, string account, int limit, TimeSpan window)
    {
        var key = "auth-limit:" + action + ":" + Fingerprint(account.Trim().ToUpperInvariant());
        lock (gate)
        {
            if (!cache.TryGetValue<Counter>(key, out var counter) || counter is null) { counter = new Counter(); cache.Set(key, counter, window); }
            return ++counter.Value <= limit;
        }
    }
}
