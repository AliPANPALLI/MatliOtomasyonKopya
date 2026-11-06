namespace EnerjiIzleme.Domain.Models.ReportModels
{
    public class EnerjiRaporGrafikModel
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
        public double ReaktifGuc_L1_VAr { get; set; } = 0;
        public double ReaktifGuc_L2_VAr { get; set; } = 0;
        public double ReaktifGuc_L3_VAr { get; set; } = 0;
        public double ToplamReaktifGuc_VAr { get; set; } = 0;
        public double GucFaktoru_L1 { get; set; }
        public double GucFaktoru_L2 { get; set; }
        public double GucFaktoru_L3 { get; set; }

        public double Cos_L1 { get; set; }
        public double Cos_L2 { get; set; }
        public double Cos_L3 { get; set; }

        public double? Gerilim_L12_V { get; set; } = 0;
        public double? Gerilim_L23_V { get; set; } = 0;
        public double? Gerilim_L31_V { get; set; } = 0;
        public double? Frekans_Hz { get; set; } = 0;
        public decimal ImportAktifEnerji_kWh { get; set; } // Toplam fark
        public decimal ImportOrtalamaEnerji_kWh { get; set; } // Toplam fark / saat
        public decimal ExportAktifEnerji_kWh { get; set; } // Toplam fark
        public List<EnerjiRaporGrafikCihazDetayModel>? CihazDetaylar { get; set; }
    }
}