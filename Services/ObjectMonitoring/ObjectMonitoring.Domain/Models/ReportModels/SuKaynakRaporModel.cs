namespace ObjectMonitoring.Domain.Models.ReportModels
{
    public class SuKaynakRaporModel
    {
        public string LokasyonName { get; set; }
        public string Bolum { get; set; } = string.Empty;
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
