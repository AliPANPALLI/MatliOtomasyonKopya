namespace Ticket.Domain.Models
{
    public class TicketLogs
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string? YapilanDegisiklik { get; set; }
        public int? UserId { get; set; }
        public string? MatliUserId { get; set; }
        public DateTime KayitZamani { get; set; }
    }
}
