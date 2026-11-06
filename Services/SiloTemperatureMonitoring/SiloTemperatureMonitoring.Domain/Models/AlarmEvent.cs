namespace SiloTemperatureMonitoring.Domain.Models
{
    public class AlarmEvent
    {
        public Guid Id { get; set; }
        public Guid AlarmRuleId { get; set; }
        public Guid SensorId { get; set; }
        public DateTime StartedAt { get; set; }
    }
}
