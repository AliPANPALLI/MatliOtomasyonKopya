namespace SiloSicaklikIzleme.Domain.Models
{
    public class TemperatureReading
    {
        public Guid SensorId { get; set; }
        public DateTime MeasuredAt { get; set; }
        public double Temperature { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
