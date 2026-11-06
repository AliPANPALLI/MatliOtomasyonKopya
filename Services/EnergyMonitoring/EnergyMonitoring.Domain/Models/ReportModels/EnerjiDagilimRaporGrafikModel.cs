namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class EnerjiDagilimRaporGrafikModel
    {
        public int CihazId { get; set; }
        public string? CihazName { get; set; }
        public string? MakineName { get; set; }
        public string? BolumName { get; set; }
        public DateTime OkunmaZamani { get; set; }
        public double? L1_V { get; set; } = 0;
        public double? L2_V { get; set; } = 0;
        public double? L3_V { get; set; } = 0;
        public double? L1_A { get; set; } = 0;
        public double? L2_A { get; set; } = 0;
        public double? L3_A { get; set; } = 0;
        public double? Notr_A { get; set; } = 0;
        public double? Toplam_Akim_A { get; set; } = 0;
        public double? AktifGuc_L1_W { get; set; } = 0;
        public double? AktifGuc_L2_W { get; set; } = 0;
        public double? AktifGuc_L3_W { get; set; } = 0;
        public double? ToplamAktifGuc_W { get; set; } = 0;
        //public double? Gerilim_L12_V { get; set; } = 0;
        //public double? Gerilim_L23_V { get; set; } = 0;
        //public double? Gerilim_L31_V { get; set; } = 0;
        public double? Frekans_Hz { get; set; } = 0;
        public decimal ImportAktifEnerji_kWh { get; set; } // Toplam fark
        public decimal ImportOrtalamaEnerji_kWh { get; set; } // Toplam fark / saat
        public List<EnerjiRaporGrafikCihazDetayModel>? CihazDetaylar { get; set; }
    }
}