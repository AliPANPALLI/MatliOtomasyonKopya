using System.Collections.Concurrent;

namespace PoultryHouseScada.API.SignalR.KumesRapor
{
    public static class KumesRaporGroupTracker
    {
        // KumesTablo için: lokasyonId -> aktif bağlantı sayısı
        public static readonly ConcurrentDictionary<int, int> KumesTabloLokasyonSayaci
            = new ConcurrentDictionary<int, int>();

        // TesisGenel için: lokasyonId -> aktif bağlantı sayısı
        public static readonly ConcurrentDictionary<int, int> TesisGenelLokasyonSayaci
            = new ConcurrentDictionary<int, int>();

        public static void IncKumesTablo(int lokasyonId)
            => KumesTabloLokasyonSayaci.AddOrUpdate(lokasyonId, 1, (_, c) => c + 1);

        public static void DecKumesTablo(int lokasyonId)
        {
            if (KumesTabloLokasyonSayaci.TryGetValue(lokasyonId, out var c))
            {
                c = Math.Max(c - 1, 0);
                if (c == 0) KumesTabloLokasyonSayaci.TryRemove(lokasyonId, out _);
                else KumesTabloLokasyonSayaci[lokasyonId] = c;
            }
        }

        public static void IncTesisGenel(int lokasyonId)
            => TesisGenelLokasyonSayaci.AddOrUpdate(lokasyonId, 1, (_, c) => c + 1);

        public static void DecTesisGenel(int lokasyonId)
        {
            if (TesisGenelLokasyonSayaci.TryGetValue(lokasyonId, out var c))
            {
                c = Math.Max(c - 1, 0);
                if (c == 0) TesisGenelLokasyonSayaci.TryRemove(lokasyonId, out _);
                else TesisGenelLokasyonSayaci[lokasyonId] = c;
            }
        }
    }
}