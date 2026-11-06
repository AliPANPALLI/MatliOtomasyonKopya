namespace Ticket.Domain.Models
{
    public class TicketDurumLog
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int DurumIdOncesi { get; set; }
        public int DurumIdSonrasi { get; set; }
        public int DurumIdTedarikciOncesi { get; set; }
        public int DurumIdTedarikciSonrasi { get; set; }
        public int? UserId { get; set; }
        public string? MatliUserId { get; set; }
        public DateTime KayitZamani { get; set; }
    }
}
