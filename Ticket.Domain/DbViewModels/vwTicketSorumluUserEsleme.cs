namespace Ticket.Domain.DbViewModels
{
    public class vwTicketSorumluUserEsleme
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int TedarikciUserId { get; set; }
        public bool? Silindi { get; set; }
        public string? UserFullName { get; set; }
        public string? Email { get; set; }
    }
}
