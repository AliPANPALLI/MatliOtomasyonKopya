namespace Ticket.Domain.DbViewModels
{
    public class vwTicketMatliTakipciEsleme
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string MatliUserId { get; set; }
        public bool? Silindi { get; set; }
        public string? UserFullName { get; set; }
        public string? Email { get; set; }
    }
}
