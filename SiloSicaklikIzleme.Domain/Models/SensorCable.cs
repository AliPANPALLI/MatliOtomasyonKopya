namespace SiloSicaklikIzleme.Domain.Models
{
    public class SensorCable
    {
        public Guid Id { get; set; }
        public Guid DepotId { get; set; }
        public Guid ControllerId { get; set; }
        public string CableNo { get; set; } = string.Empty;
    }
}
