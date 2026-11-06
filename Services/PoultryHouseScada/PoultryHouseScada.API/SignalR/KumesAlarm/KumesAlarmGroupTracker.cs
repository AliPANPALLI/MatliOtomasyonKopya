using System.Collections.Concurrent;

namespace PoultryHouseScada.API.SignalR.KumesAlarm
{
    public static class KumesAlarmGroupTracker
    {
        // KumesAlarmlar için: (kumesId, userId) -> aktif bağlantı sayısı
        public static readonly ConcurrentDictionary<(int kumesId, string userId), int> KumesAlarmUserCounts
            = new ConcurrentDictionary<(int, string), int>();

        public static void IncKumesAlarmlar(int kumesId, string userId)
            => KumesAlarmUserCounts.AddOrUpdate((kumesId, userId), 1, (_, c) => c + 1);

        public static void DecKumesAlarmlar(int kumesId, string userId)
        {
            var key = (kumesId, userId);
            if (KumesAlarmUserCounts.TryGetValue(key, out var c))
            {
                c = Math.Max(c - 1, 0);
                if (c == 0) KumesAlarmUserCounts.TryRemove(key, out _);
                else KumesAlarmUserCounts[key] = c;
            }
        }
    }
}