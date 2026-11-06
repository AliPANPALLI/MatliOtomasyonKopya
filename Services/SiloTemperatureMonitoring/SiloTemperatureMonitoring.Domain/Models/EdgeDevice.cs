namespace SiloTemperatureMonitoring.Domain.Models
{
    public class EdgeDevice
    {
        public Guid Id { get; set; }
        public Guid FacilityId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public DateTime? LastSeenAt { get; set; }
    }
}
