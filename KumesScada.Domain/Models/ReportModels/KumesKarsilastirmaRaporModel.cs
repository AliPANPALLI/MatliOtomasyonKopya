namespace KumesScada.Domain.Models.ReportModels
{
    public class KumesKarsilastirmaRaporModel
    {
        public int KumesId { get; set; }
        public string? KumesName { get; set; }
        public string? KumesKodu { get; set; }
        public List<KumesRaporModel> Rapor { get; set; }
    }
}