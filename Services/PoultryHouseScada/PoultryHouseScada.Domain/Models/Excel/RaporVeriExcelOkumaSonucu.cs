namespace PoultryHouseScada.Domain.Models.Excel
{
    public class RaporVeriHaftalikDeger
    {
        public int HaftaSayisi { get; set; }

        public decimal RaporHedefDeger { get; set; }
    }

    public class RaporVeriExcelOkumaSonucu
    {
        public List<RaporVeriHaftalikDeger> HedefYuzde { get; set; } = new();

        public List<RaporVeriHaftalikDeger> HedefGram { get; set; } = new();

        // "VERİLER" sayfasındaki diğer HEDEF bölümleri (bkz.
        // RaporVeriExcelReader) — %/GR dışında, Grafik sekmesine henüz
        // bağlanmamış ama okunup kaydediliyor (bkz. RaporVeriAppService).
        public List<RaporVeriHaftalikDeger> HedefAdet { get; set; } = new();

        public List<RaporVeriHaftalikDeger> HedefYasam { get; set; } = new();

        public List<RaporVeriHaftalikDeger> HedefHh { get; set; } = new();

        public List<RaporVeriHaftalikDeger> HedefEggMass { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }
}
