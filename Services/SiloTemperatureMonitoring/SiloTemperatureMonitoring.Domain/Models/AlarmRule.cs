namespace SiloTemperatureMonitoring.Domain.Models
{
    public class AlarmRule
    {
        public Guid Id { get; set; }
        public Guid? DepotId { get; set; }
        public Guid? SensorId { get; set; }
        public double WarningThreshold { get; set; }
        public double CriticalThreshold { get; set; }
    }
}
