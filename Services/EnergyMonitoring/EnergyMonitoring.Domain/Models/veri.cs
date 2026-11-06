using MongoDB.Bson;

namespace EnergyMonitoring.Domain.Models
{
    public class veri
    {
        public double? L1_V { get; set; } = 0;
        public double? L2_V { get; set; } = 0;
        public double? L3_V { get; set; } = 0;
        public double? L1_A { get; set; } = 0;
        public double? L2_A { get; set; } = 0;
        public double? L3_A { get; set; } = 0;
        public double? Toplam_Akim_A { get; set; } = 0;
        public double? AktifGuc_L1_W { get; set; } = 0;
        public double? AktifGuc_L2_W { get; set; } = 0;
        public double? AktifGuc_L3_W { get; set; } = 0;
        public double? ReaktifGuc_L1_VAr { get; set; } = 0;
        public double? ReaktifGuc_L2_VAr { get; set; } = 0;
        public double? ReaktifGuc_L3_VAr { get; set; } = 0;
        public double? GorunurGuc_L1_VA { get; set; } = 0;
        public double? GorunurGuc_L2_VA { get; set; } = 0;
        public double? GorunurGuc_L3_VA { get; set; } = 0;
        public double? GucFaktoru_L1 { get; set; } = 0;
        public double? GucFaktoru_L2 { get; set; } = 0;
        public double? GucFaktoru_L3 { get; set; } = 0;
        public double? Cos_L1 { get; set; } = 0;
        public double? Cos_L2 { get; set; } = 0;
        public double? Cos_L3 { get; set; } = 0;
        public double? Gerilim_L12_V { get; set; } = 0;
        public double? Gerilim_L23_V { get; set; } = 0;
        public double? Gerilim_L31_V { get; set; } = 0;
        public double? Frekans_Hz { get; set; } = 0;
        public double? ToplamAktifGuc_W { get; set; } = 0;
        public double? ToplamReaktifGuc_VAr { get; set; } = 0;
        public double? ToplamGorunurGuc_VA { get; set; } = 0;
        public Decimal128? Import_Aktif_Enerji_Wh { get; set; }
        public Decimal128? Export_Aktif_Enerji_Wh { get; set; }
    }
}
