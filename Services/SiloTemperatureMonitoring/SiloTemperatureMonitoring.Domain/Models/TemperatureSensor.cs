namespace SiloTemperatureMonitoring.Domain.Models
{
    public class TemperatureSensor
    {
        public Guid Id { get; set; }
        public Guid SensorCableId { get; set; }
        public int SensorNo { get; set; }
        public string SensorKey { get; set; } = string.Empty;
    }
}
