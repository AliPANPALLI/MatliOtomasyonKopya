namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class EnerjiRaporGrafikCihazDetayModel
    {
        public int CihazId { get; set; }
        public string? CihazName { get; set; }
        public string? MakineName { get; set; }
        public string? BolumName { get; set; }
        public double? L1_A { get; set; } = 0;
        public double? L2_A { get; set; } = 0;
        public double? L3_A { get; set; } = 0;
        public double? Toplam_Akim_A { get; set; } = 0;
        public double? ToplamAktifGuc_W { get; set; } = 0;
        public decimal ImportAktifEnerji_kWh { get; set; }
        public decimal ImportOrtalamaEnerji_kWh { get; set; }
        public decimal ExportAktifEnerji_kWh { get; set; }
        public DateTime? MinTime { get; set; }
        public DateTime? MaxTime { get; set; }
    }
}