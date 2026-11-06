namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class TuketimEnerjiVerileriTabloRaporModel
    {
        public string Ozellik { get; set; }
        public string? AktifEnerji { get; set; }
        public string? OrtalamaEnerji { get; set; }
        public DateTime? IlkOkumaZaman { get; set; }
        public DateTime? SonOkumaZaman { get; set; }
    }
}
