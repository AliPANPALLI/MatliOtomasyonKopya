namespace EnerjiIzleme.Domain.Models
{
    public class AlarmLog
    {
        public int Id { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? Mesaj { get; set; }
        public int? AlarmId { get; set; }
        public byte? AlarmSeviye { get; set; }
        public bool? Aktif { get; set; }
        public DateTime? PasifZamani { get; set; }
        public bool? Silindi { get; set; }
        public int? CihazId { get; set; }
    }
}
