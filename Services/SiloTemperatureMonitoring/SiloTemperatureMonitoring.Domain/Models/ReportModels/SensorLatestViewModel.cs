namespace SiloTemperatureMonitoring.Domain.Models.ReportModels
{
    public class SensorLatestViewModel
    {
        public Guid SensorId { get; set; }
        public int SensorNo { get; set; }
        public string SensorKey { get; set; } = string.Empty;
        public Guid SensorCableId { get; set; }
        public string? CableNo { get; set; }
        public Guid? DepotId { get; set; }
        public string? TesisKodu { get; set; }
        public string? TesisAdi { get; set; }
        public string? DepotAdi { get; set; }
        public int? DepotNo { get; set; }
        public double? Temperature { get; set; }
        public DateTime? MeasuredAt { get; set; }
    }
}
