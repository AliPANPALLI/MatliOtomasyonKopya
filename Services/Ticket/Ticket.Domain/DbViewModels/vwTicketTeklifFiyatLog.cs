namespace Ticket.Domain.DbViewModels
{
    public class vwTicketTeklifFiyatLog
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public decimal? TeklifFiyatOncesi { get; set; }
        public int? TeklifParaBirimiIdOncesi { get; set; }
        public string? TeklifParaBirimiNameOncesi { get; set; }
        public decimal? TeklifFiyatSonrasi { get; set; }
        public int? TeklifParaBirimiIdSonrasi { get; set; }
        public string? TeklifParaBirimiNameSonrasi { get; set; }
        public int? UserId { get; set; }
        public string? MatliUserId { get; set; }
        public DateTime KayitZamani { get; set; }
        public string? UserFullName { get; set; }
        public string? TeklifOncesiText { get; set; }
        public string? TeklifSonrasiText { get; set; }
    }
}