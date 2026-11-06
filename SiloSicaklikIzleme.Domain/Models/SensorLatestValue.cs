namespace SiloSicaklikIzleme.Domain.Models
{
    public class SensorLatestValue
    {
        public Guid SensorId { get; set; }
        public double Temperature { get; set; }
        public DateTime MeasuredAt { get; set; }
    }
}
