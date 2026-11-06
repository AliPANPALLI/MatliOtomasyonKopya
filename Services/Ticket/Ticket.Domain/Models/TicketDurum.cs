namespace Ticket.Domain.Models
{
    public class TicketDurum
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Aciklama { get; set; }
        public int? Sira { get; set; }
        public bool? TedarikciGorsun { get; set; }
        public bool? TakipciGorsun { get; set; }
        public string? BackColor { get; set; }
        public string? TextColor { get; set; }
    }
}