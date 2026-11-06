namespace SharedApp.Domain.Models
{
    public class GonderilenSms
    {
        public int Id { get; set; }
        public DateTime KayitZamani { get; set; }
        public string CepTelefonu { get; set; }
        public string Mesaj { get; set; }
        public string? DonusMesaj { get; set; }
        public string? NetIp { get; set; }
        public bool? BasariliMi { get; set; }
        public int? SeviyeNo { get; set; }
        public string? UserId { get; set; }
        public DateTime? SonDenemeZamani { get; set; }
    }
}
