namespace Ticket.Domain.Models
{
    public class TicketMatliTakipciEsleme
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string MatliUserId { get; set; }
        public bool? Silindi { get; set; }
    }
}
