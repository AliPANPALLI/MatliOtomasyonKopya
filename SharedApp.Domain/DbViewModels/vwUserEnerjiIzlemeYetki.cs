namespace SharedApp.Domain.DbViewModels
{
    public class vwUserEnerjiIzlemeYetki
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int LokasyonId { get; set; }
        public int SirketId { get; set; }
        public string? Name { get; set; }
        public bool? UserSilindi { get; set; }
        public string? Email { get; set; }
        public string? Surname { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserName { get; set; }
        public bool? Read { get; set; }
        public bool? Write { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? Aktif { get; set; }
        public bool? Onaylayabilir { get; set; }
    }
}
