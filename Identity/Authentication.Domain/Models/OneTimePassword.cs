namespace Authentication.Domain.Models
{
    public class OneTimePassword
    {
        public int Id { get; set; }
        public DateTime? EklenmeTarihi { get; set; }
        public string? EkleyenUserId { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
        public string? GuncelleyenUserId { get; set; }
        public string? OtpSecretCode { get; set; }
        public string? OtpCode { get; set; }
        public string? UserId { get; set; }
        public bool? Aktif { get; set; }
        public bool? Kullanildi { get; set; }
        public string? IP { get; set; }
        public bool? Domain { get; set; }
        public byte? DenemeSayisi { get; set; }
    }
}
