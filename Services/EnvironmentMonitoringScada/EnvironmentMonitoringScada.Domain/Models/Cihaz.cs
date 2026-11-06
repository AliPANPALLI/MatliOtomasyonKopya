using System.Text.Json.Serialization;

namespace EnvironmentMonitoringScada.Domain.Models
{
    public class Cihaz
    {
        public int Id { get; set; }
        public int OdaId { get; set; }
        public int SensorIdMQTT { get; set; }
        public string? Marka { get; set; }
        public string? Model { get; set; }
        public string? SensorAdi { get; set; }
        public DateTime? KayitZamani { get; set; }

        [JsonIgnore] public virtual Oda Oda { get; set; }
    }
}
