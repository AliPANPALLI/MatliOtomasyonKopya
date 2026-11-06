namespace Ticket.Domain.Models
{
    public class TicketTanim
    {
        public int Id { get; set; }
        public string OlusturanUserId { get; set; }
        public int FormId { get; set; }
        public int TedarikciId { get; set; }
        public int LokasyonId { get; set; }
        public string Tanim { get; set; }
        public string KisaTanim { get; set; }
        public DateTime KayitZamani { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
        public DateTime? TeklifKararDurumGuncellemeZamani { get; set; }
        public string? TeklifKararDurumGuncelleyenUserId { get; set; }
        public string? GuncellemedeYapilanIslem { get; set; }
        public int DurumId { get; set; }
        public int DurumIdTedarikci { get; set; }
        public decimal? TeklifFiyat { get; set; }
        public int? TeklifParaBirimiId { get; set; }
        public bool? TeklifSabitle { get; set; }
        public byte? TeklifKararDurumu { get; set; }
        public string? SasNo { get; set; }
        public bool? Silindi { get; set; }
        public bool? FaturaKesildi { get; set; }
    }
}
