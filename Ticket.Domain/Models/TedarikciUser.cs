namespace Ticket.Domain.Models
{
    public class TedarikciUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public int TedarikciId { get; set; }
        public string? LoginUserId { get; set; }//eğer kullanici açılmışsa buradan eşlenecek
        public bool? FiyatGorsun { get; set; }
        public bool? FiyatDuzenleyebilsin { get; set; }
        public bool? Silindi { get; set; }
    }
}