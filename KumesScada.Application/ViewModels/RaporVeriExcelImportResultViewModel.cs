namespace KumesScada.Application.ViewModels
{
    public class RaporVeriExcelImportResultViewModel
    {
        public string KumesNo { get; set; } = string.Empty;

        public int HedefYuzdeSatirSayisi { get; set; }

        public int HedefGramSatirSayisi { get; set; }

        // Bkz. RaporVeriExcelReader üzerindeki not — okunup kaydediliyor
        // ama henüz Grafik sekmesinde gösterilmiyor.
        public int HedefAdetSatirSayisi { get; set; }

        public int HedefYasamSatirSayisi { get; set; }

        public int HedefHhSatirSayisi { get; set; }

        public int HedefEggMassSatirSayisi { get; set; }

        public List<string> Warnings { get; set; } = new();
    }
}
