namespace Ticket.Domain.DbViewModels
{
    public class vwTicketTanim
    {
        public int Id { get; set; }
        public string OlusturanUserId { get; set; }
        public string? OlusturanUserFullName { get; set; }
        public int FormId { get; set; }
        public int TedarikciId { get; set; }
        public string? TedarikciName { get; set; }
        public int LokasyonId { get; set; }
        public string? LokasyonName { get; set; }
        public string Tanim { get; set; }
        public string KisaTanim { get; set; }
        public DateTime KayitZamani { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public int DurumId { get; set; }
        public string DurumName { get; set; }
        public int? DurumIdTedarikci { get; set; }
        public string? DurumNameTedarikci { get; set; }
        public decimal? TeklifFiyat { get; set; }
        public int? TeklifParaBirimiId { get; set; }
        public bool? TeklifSabitle { get; set; }
        public string? ParaBirimi { get; set; }
        public string? SasNo { get; set; }
        public byte? TeklifKararDurumu { get; set; }
        public string? TeklifKararDurumGuncelleyenUserFullName { get; set; }
        public DateTime? TeklifKararDurumGuncellemeZamani { get; set; }
        public bool? Silindi { get; set; }
        public bool? FaturaKesildi { get; set; }
    }
}
