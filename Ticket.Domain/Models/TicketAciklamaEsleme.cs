namespace Ticket.Domain.Models
{
    public class TicketAciklamaEsleme
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? UserId { get; set; }
        public string? MatliUserId { get; set; }
        public string LoginUserId { get; set; }
        public string? Aciklama { get; set; }
        public DateTime? KayitZamani { get; set; }
    }
}
