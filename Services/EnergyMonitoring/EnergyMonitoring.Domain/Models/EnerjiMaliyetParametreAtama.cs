namespace EnergyMonitoring.Domain.Models
{
    public class EnerjiMaliyetParametreAtama
    {
        public int Id { get; set; }
        public int EnerjiMaliyetGrupId { get; set; }
        public int NodeId { get; set; }
        public string? NodeType { get; set; }
        public DateTime? KayitZamani { get; set; }
        public bool? Aktif { get; set; }
        public bool? Silindi { get; set; }
    }
}