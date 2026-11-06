namespace PoultryHouseScada.Domain.Models.ReportModels
{
    internal class TestModel
    {
    }
    // Ölüm İstatistikleri
    public class DeathStatsDto
    {
        public int? Today { get; set; }
        public int? Yesterday { get; set; }
        public int? Total10 { get; set; }
        public List<double>? Trend { get; set; } // Grafik verisi
    }

    // Su ve Yem için Ortak Yapı (Resource)
    public class ResourceStatsDto
    {
        public double? Today { get; set; }       // Toplam Tüketim (ton ve m3)
        public double? TodayPer { get; set; }    // Hayvan Başı (gr ve ml)
        public double? Total10 { get; set; }     // 10 Günlük Toplam (ton ve m3)
        public double? Total10Per { get; set; }  // 10 Günlük H.Başı Ort. (gr ve ml)

        public List<double>? TrendTotal { get; set; } // Grafik: Toplam Çubuğu
        public List<double>? TrendPer { get; set; }   // Grafik: H.Başı Çizgisi
    }

    // Yumurta İstatistikleri
    public class EggStatsDto
    {
        public int? Today { get; set; }          // Adet olduğu için int/long
        public double? TodayPer { get; set; }    // Oran (0.84 gibi)
        public int? Total10 { get; set; }
        public double? Total10Per { get; set; }
        public List<double> Trend { get; set; } // Grafik verisi
    }

    // Ana Liste Elemanı
    public class FarmDataDto
    {
        public string? Id { get; set; }       // Örn: "rahmiye"
        public string? Name { get; set; }     // Örn: "Rahmiye Çiftlik"
        public long? Pop { get; set; }        // Nüfus
        public string? Stock { get; set; }    // Stok (Örn: "999 ton")

        // Kategoriler
        public DeathStatsDto? Death { get; set; }
        public ResourceStatsDto? Water { get; set; }
        public ResourceStatsDto? Feed { get; set; }
        public ResourceStatsDto? Energy { get; set; }
        public EggStatsDto? Egg { get; set; }
    }

    public sealed class AnimalCountRangeRequest
    {
        public int[] NodeIds { get; set; } = Array.Empty<int>();
        public DateTime StartDate { get; set; }   // dahil
        public DateTime EndDate { get; set; }     // dahil
    }

    public sealed class AnimalCountRangeItem
    {
        public int NodeId { get; set; }
        public DateTime Date { get; set; }
        public long Count { get; set; }
    }

    //public sealed class AnimalCountRangeResponse
    //{
    //    public List<AnimalCountRangeItem> Items { get; set; } = new();
    //}
}

