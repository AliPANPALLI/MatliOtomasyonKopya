namespace KumesScada.Domain.Models.ReportModels
{
    public class TesisGenelKartModel
    {
        public int? CanliHayvanAdet { get; set; }
        public int? BugunOlenHayvanAdet { get; set; }
        public int? DunOlenHayvanAdet { get; set; }
        public int? AktifKumesSayisi { get; set; }
        public int? KumesSayisi { get; set; }
        public decimal? OrtalamaSicaklik { get; set; }
        public decimal? OrtalamaNem { get; set; }
        public decimal? ToplamYemTuketim { get; set; }
        public decimal? HayvanBasiYemTuketim { get; set; }
        public decimal? ToplamSuTuketim { get; set; }
        public decimal? HayvanBasiSuTuketim { get; set; }
        public decimal? PetSuTuketim { get; set; }
        public int? YumurtaAdet { get; set; }
        public decimal? HayvanBasiYumurtaAdet { get; set; }
    }
}