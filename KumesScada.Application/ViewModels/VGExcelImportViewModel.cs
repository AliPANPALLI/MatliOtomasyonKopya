namespace KumesScada.Application.ViewModels
{
    public class VGExcelImportViewModel
    {
        public string FileName { get; set; } = string.Empty;

        public DateTime ImportDate { get; set; }

        public int TotalRecordCount { get; set; }

        public List<VGExcelKumesGunlukViewModel> Data { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }

    public class VGExcelKumesGunlukViewModel
    {
        public DateTime Tarih { get; set; }

        public string KumesNo { get; set; } = string.Empty;

        public decimal? Olum { get; set; }

        public decimal? YumurtaAdeti { get; set; }

        public decimal? YumurtaGramaji { get; set; }

        public decimal? CanliAgirlik { get; set; }

        public decimal? SaglamYumurta { get; set; }

        public decimal? KirikYumurta { get; set; }

        public decimal? KirliYumurta { get; set; }

        public decimal? KucukKirik { get; set; }

        public decimal? Fire { get; set; }

        public decimal? GizliCatlak { get; set; }

        public decimal? YemTuketim { get; set; }

        public decimal? YemTuketimHayvanBasi { get; set; }

        public decimal? SuTuketim { get; set; }

        public decimal? Randiman { get; set; }

        public decimal? VbapRandiman { get; set; }

        public string? Uygulama { get; set; }
    }
}