namespace Ticket.Domain.Models
{
    public class TicketSorumluUserEsleme
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int TedarikciUserId { get; set; }
        public bool? Silindi { get; set; }
    }
}
