namespace SiloTemperatureMonitoring.Domain.Models.ReportModels
{
    public class AktifAlarmViewModel
    {
        public Guid AlarmEventId { get; set; }
        public Guid AlarmRuleId { get; set; }
        public Guid SensorId { get; set; }
        public string? SensorKey { get; set; }
        public string? CableNo { get; set; }
        public Guid? DepotId { get; set; }
        public string? TesisKodu { get; set; }
        public string? TesisAdi { get; set; }
        public string? DepotAdi { get; set; }
        public DateTime StartedAt { get; set; }
        public double WarningThreshold { get; set; }
        public double CriticalThreshold { get; set; }
    }
}
