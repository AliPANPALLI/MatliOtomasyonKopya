namespace Authentication.Domain.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string? SicilNo{ get; set; }
        public DateTimeOffset PasswordChangedAt { get; set; }
        //public string? RefreshToken { get; set; }
        //public DateTime? RefreshTokenEndDate { get; set; }
        public bool? IsLocked { get; set; }
        public byte? IncorrectLoginNumber { get; set; }
        /// <summary>
        /// Kullanicinin hangi lokasyonda calistigini gormek icin kullanilacak
        /// </summary>
        public int LokasyonId { get; set; }
        public byte[]? ProfilePhoto { get; set; }
        public string Position { get; set; }
        public bool? RequiresOtp { get; set; }
        public bool? Silindi { get; set; }
        public int? TokenExpirationTime { get; set; }
        public bool? Tedarikci{ get; set; }
        public bool? NeverSendOTP{ get; set; }

        //public bool? EmailConfirmed { get; set; }
        //public bool? PhoneNumberConfirmed { get; set; }
    }
}
