namespace PoultryHouseScada.Domain.Models
{
    public class KonveyorRapor
    {
        public int Id { get; set; }
        public int? KonveyorId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public bool? Calisiyor { get; set; }
        public bool? Ariza { get; set; }
        public bool? Bosta { get; set; }
        public byte? Durum { get; set; }
        public string? Aciklama { get; set; }
        public bool? VeriYok { get; set; }
    }
}
