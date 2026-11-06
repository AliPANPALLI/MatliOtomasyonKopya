namespace EnerjiIzleme.Domain.Models.ReportModels
{
    public class SahaVerileriTabloRaporModel
    {
        public string Ozellik { get; set; }
        public string? Toplam { get; set; }
        public string? L1 { get; set; }
        public string? L2 { get; set; }
        public string? L3 { get; set; }
        public string? Notr { get; set; }
        public DateTime? OkunmaZamani { get; set; }
        public int? Durum { get; set; }
    }

    public class SahaVerileriTabloModel
    {
        public List<SahaVerileriTabloRaporModel>? SahaVerileriTabloRaporModels { get; set; } = new();
        public string? UyariVerenAnalizorler { get; set; }
    }
}