namespace Ticket.Domain.Models
{
    public class TakipciUser
    {
        public int Id { get; set; }
        public string LoginUserId { get; set; }
        public bool? SadeceAciklamaYapar { get; set; }
        public bool? SadeceGoruntuler { get; set; }
        public bool? Silindi { get; set; }
    }
}