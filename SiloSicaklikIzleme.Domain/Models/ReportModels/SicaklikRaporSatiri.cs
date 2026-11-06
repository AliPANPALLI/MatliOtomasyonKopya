namespace SiloSicaklikIzleme.Domain.Models.ReportModels
{
    public class SicaklikRaporSatiri
    {
        public Guid SensorId { get; set; }
        public string? SensorKey { get; set; }
        public string? CableNo { get; set; }
        public Guid? DepotId { get; set; }
        public string? TesisKodu { get; set; }
        public string? TesisAdi { get; set; }
        public string? DepotAdi { get; set; }
        public DateTime Bucket { get; set; }
        public double? OrtSicaklik { get; set; }
        public double? MinSicaklik { get; set; }
        public double? MaksSicaklik { get; set; }
        public long? OrnekSayisi { get; set; }
    }
}
