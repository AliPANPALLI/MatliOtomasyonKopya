namespace Ticket.Domain.Models
{
    public class TicketTeklifFiyatLog
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public decimal? TeklifFiyatOncesi { get; set; }
        public int? TeklifParaBirimiIdOncesi { get; set; }
        public decimal? TeklifFiyatSonrasi { get; set; }
        public int? TeklifParaBirimiIdSonrasi { get; set; }
        public int? UserId { get; set; }
        public string? MatliUserId { get; set; }
        public DateTime KayitZamani { get; set; }
    }
}