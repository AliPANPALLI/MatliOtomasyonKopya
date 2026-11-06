namespace SuHatScada.Domain.Models.ReportModels
{
    //public class SuKaynakRaporModel
    //{
    //    public DateTime? KayitZamani { get; set; }
    //    public string? LokasyonName { get; set; }
    //    public string? SuKaynakName { get; set; }
    //    public decimal? SuUretimi { get; set; }
    //    public decimal? SuTuketimi { get; set; }
    //    public decimal? CalismaSuresi { get; set; }
    //}

    public class SuKaynakRaporModel
    {
        public string LokasyonName { get; set; }
        public string Kategori { get; set; }
        public string Nokta { get; set; }
        public Dictionary<string, decimal> ZamansalVeri { get; set; } = new();
    }

    public class TimePeriodData
    {
        public string PeriyotName { get; set; }
        public DateTime Periyot { get; set; }  // Gün, hafta, ay, yıl
        public decimal Toplam { get; set; }  // Bu döneme ait toplam
    }
}