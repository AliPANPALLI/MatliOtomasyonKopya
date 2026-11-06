namespace SiloSicaklikIzleme.Domain.DbViewModels
{
    // TEMPERATURE_HOURLY / TEMPERATURE_DAILY view'leri aynı şekle sahip (TimescaleDB
    // continuous aggregate). Her periyot ayrı CLR tipiyle eşleniyor ki EF Core her
    // birini kendi view'ine (HasNoKey + ToView) bağlayabilsin.
    public abstract class TemperatureBucketBase
    {
        public Guid SensorId { get; set; }
        public DateTime Bucket { get; set; }
        public double? AvgTemperature { get; set; }
        public double? MinTemperature { get; set; }
        public double? MaxTemperature { get; set; }
        public long? ReadingCount { get; set; }
    }

    public class TemperatureHourly : TemperatureBucketBase
    {
    }

    public class TemperatureDaily : TemperatureBucketBase
    {
    }
}
