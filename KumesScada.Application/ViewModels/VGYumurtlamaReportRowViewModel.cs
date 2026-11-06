namespace KumesScada.Application.ViewModels
{
    public sealed class VGYumurtlamaReportRowViewModel
    {
        public DateTime Tarih { get; set; }

        public string KumesNo { get; set; } = string.Empty;

        // Yumurtlama kartındaki karşılıklar

        public decimal? GunlukOlum { get; set; }

        public decimal? OlumOrani { get; set; }

        public decimal? TavukSayisi { get; set; }

        public decimal? GizliCatlak { get; set; }

        public decimal? SaglamYumurta { get; set; }

        public decimal? KirikYumurta { get; set; }

        public decimal? KirliYumurta { get; set; }

        public decimal? KucukKirik { get; set; }

        public decimal? Fire { get; set; }

        public decimal? ToplamYumurta { get; set; }

        public decimal? HdRandiman { get; set; }

        public decimal? YumurtaGramaji { get; set; }

        public decimal? GunlukYumurtaKutlesi { get; set; }

        public decimal? YemKg { get; set; }

        public decimal? HayvanBasiYemGr { get; set; }

        public decimal? SuLitre { get; set; }

        public decimal? SuYemOrani { get; set; }

        public decimal? YumurtaBasiYemGr { get; set; }

        public decimal? CanliAgirlik { get; set; }

        public decimal? TekSiraRandiman { get; set; }

        public decimal? VbapRandiman { get; set; }

        public string? Uygulama { get; set; }
    }
}