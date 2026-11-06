using System.Collections.Concurrent;

namespace PoultryHouseScada.API.SignalR.Test
{
    public static class TestTracker
    {
        // kumesId -> aktif bağlantı sayısı
        public static readonly ConcurrentDictionary<int, int> KumesAlarmUserCounts = new();

        // connectionId -> hangi kumesId'lere girdi
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, byte>> _connKumesMap = new();

        public static void Join(string connectionId, int kumesId)
        {
            // Aynı connection aynı kumes'e 2 kez join olmasın
            var set = _connKumesMap.GetOrAdd(connectionId, _ => new ConcurrentDictionary<int, byte>());
            if (set.TryAdd(kumesId, 0))
            {
                KumesAlarmUserCounts.AddOrUpdate(kumesId, 1, (_, c) => c + 1);
            }
        }

        public static void Leave(string connectionId, int kumesId)
        {
            if (_connKumesMap.TryGetValue(connectionId, out var set) && set.TryRemove(kumesId, out _))
            {
                DecrementOrRemove(kumesId);

                if (set.IsEmpty)
                    _connKumesMap.TryRemove(connectionId, out _);
            }
        }

        // bağlantı koptuğunda tüm join'leri düş
        public static void Disconnect(string connectionId)
        {
            if (_connKumesMap.TryRemove(connectionId, out var set))
            {
                foreach (var kumesId in set.Keys)
                    DecrementOrRemove(kumesId);
            }
        }

        private static void DecrementOrRemove(int kumesId)
        {
            KumesAlarmUserCounts.AddOrUpdate(
                kumesId,
                0,
                (_, c) =>
                {
                    var next = c - 1;
                    return next < 0 ? 0 : next;
                });

            if (KumesAlarmUserCounts.TryGetValue(kumesId, out var c) && c <= 0)
                KumesAlarmUserCounts.TryRemove(kumesId, out _);
        }
    }
}
