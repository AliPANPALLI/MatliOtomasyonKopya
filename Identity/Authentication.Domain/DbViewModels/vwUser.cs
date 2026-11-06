namespace Authentication.Domain.DbViewModels
{
    public class vwUser
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public bool? IsLocked { get; set; }
        public int LokasyonId { get; set; }
        public int SirketId { get; set; }
        public string LokasyonName { get; set; }
        public string SirketName { get; set; }
        public byte[]? ProfilePhoto { get; set; }
        public string Position { get; set; }
        public bool? RequiresOtp { get; set; }
        public bool? Silindi { get; set; }
        public bool? Tedarikci { get; set; }
        public string? UserFullName { get; set; }
        public int? TokenExpirationTime { get; set; }
    }
}