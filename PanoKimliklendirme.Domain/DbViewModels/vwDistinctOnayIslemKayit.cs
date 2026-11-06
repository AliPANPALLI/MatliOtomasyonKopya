namespace PanoKimliklendirme.Domain.DbViewModels
{
    public class vwDistinctOnayIslemKayit
    {
        public string UserId { get; set; }
        public int? OgeId { get; set; }
        public string? OgeTipi { get; set; }
        public string? OgeName { get; set; }
        public string? UserFullName { get; set; }
        public int? LokasyonId { get; set; }
        public int? PanoId { get; set; }
        public string? SirketName { get; set; }
        public string? LokasyonName { get; set; }
        public string? DosyaAd { get; set; }
        public string? KlasorAd { get; set; }
    }
}