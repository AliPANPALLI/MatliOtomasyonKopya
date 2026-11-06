namespace EnerjiIzleme.API.SignalR
{
    public interface IKumesEnerjiSubscriptionStore
    {
        void Touch(int bolumId);
        IReadOnlyCollection<int> GetActiveBolums();
    }

    public class KumesEnerjiSubscriptionStore : IKumesEnerjiSubscriptionStore
    {
        private readonly object _lock = new();
        private readonly Dictionary<int, DateTime> _lastSeen = new();

        public void Touch(int kumesTip)
        {
            lock (_lock) _lastSeen[kumesTip] = DateTime.UtcNow;
        }

        public IReadOnlyCollection<int> GetActiveBolums()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5); // 5 dk içinde istek alanlar aktif say
            lock (_lock)
            {
                var active = _lastSeen.Where(kv => kv.Value >= cutoff).Select(kv => kv.Key).ToList();
                // eski olanları temizle
                foreach (var k in _lastSeen.Where(kv => kv.Value < cutoff).Select(kv => kv.Key).ToList())
                    _lastSeen.Remove(k);
                return active;
            }
        }
    }
}
