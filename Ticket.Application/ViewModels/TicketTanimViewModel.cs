namespace Ticket.Application.ViewModels
{
    public class TicketTanimViewModel
    {
        public int Id { get; set; }
        public string OlusturanUserId { get; set; }
        public int FormId { get; set; }
        public int TedarikciId { get; set; }
        public int LokasyonId { get; set; }
        public string Tanim { get; set; }
        public string KisaTanim { get; set; }
        public DateTime KayitZamani { get; set; }
        public int DurumId { get; set; }
        public decimal? TeklifFiyat { get; set; }
        public int? TeklifParaBirimiId { get; set; }
    }
}