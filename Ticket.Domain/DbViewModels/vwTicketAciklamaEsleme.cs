namespace Ticket.Domain.DbViewModels
{
    public class vwTicketAciklamaEsleme
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? UserId { get; set; }
        public string? MatliUserId { get; set; }
        public string? Aciklama { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? UserFullName { get; set; }
        public string? LoginUserId { get; set; }
    }
}
