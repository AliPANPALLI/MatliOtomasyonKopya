namespace Ticket.Domain.DbViewModels
{
    public class vwTedarikciUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public int TedarikciId { get; set; }
        public bool? Silindi { get; set; }
        public string? UserFullName { get; set; }
        public string? LoginUserId { get; set; }
        public bool? FiyatGorsun { get; set; }
        public bool? FiyatDuzenleyebilsin { get; set; }
    }
}
