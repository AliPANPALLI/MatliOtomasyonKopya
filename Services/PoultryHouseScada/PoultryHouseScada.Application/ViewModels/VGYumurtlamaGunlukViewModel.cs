namespace PoultryHouseScada.Application.ViewModels
{
    public class VGYumurtlamaGunlukViewModel
    {
        public int Gun { get; set; }

        public int Hafta { get; set; }

        public DateTime Tarih { get; set; }

        public decimal? KabukDirenci { get; set; }

        public decimal? GizliCatlak { get; set; }

        public decimal GunlukOlum { get; set; }

        public decimal OlumOrani { get; set; }

        public decimal TavukSayisi { get; set; }

        public decimal SaglamYumurta { get; set; }

        public decimal SaglamOrani { get; set; }

        public decimal KirikYumurta { get; set; }

        public decimal KirikOrani { get; set; }

        public decimal KirliYumurta { get; set; }

        public decimal KirliOrani { get; set; }

        public decimal KucukKirik { get; set; }

        public decimal KucukKirikOrani { get; set; }

        public decimal Fire { get; set; }

        public decimal FireOrani { get; set; }

        // Ana excel'in "Yumurtlama " sayfasında sonradan eklenen
        // sütunlar — eski kartlarda/kaynaklarda null kalır.
        public decimal? LikidKabuksuzZar { get; set; }

        public decimal? LikidKabuksuzZarOrani { get; set; }

        public decimal? ZarliYumurtaKg { get; set; }

        public decimal? MobaKirikDegeri { get; set; }

        public decimal? MobaKirliDegeri { get; set; }

        public string? KumesPersoneli { get; set; }

        public string? YeminRasyonNo { get; set; }

        public decimal ToplamYumurta { get; set; }

        public decimal HdRandiman { get; set; }

        public decimal HhRandiman { get; set; }

        public decimal? YumurtaGramaji { get; set; }

        public decimal? GunlukYumurtaKutlesi { get; set; }

        public decimal? CanliAgirlik { get; set; }

        public decimal YemKg { get; set; }

        public decimal HayvanBasiYemGr { get; set; }

        public decimal SuLitre { get; set; }

        public decimal SuYemOrani { get; set; }

        public decimal? YumurtaBasiYemGr { get; set; }

        public decimal? Hu { get; set; }

        public decimal? TekSiraRandiman { get; set; }

        public decimal? VbapRandiman { get; set; }

        public string? Uygulama { get; set; }

        public decimal? Klavuz { get; set; }

        public decimal? KlavuzOrani { get; set; }

        public decimal? Small { get; set; }

        public decimal? SmallOrani { get; set; }

        public decimal? Medium { get; set; }

        public decimal? MediumOrani { get; set; }

        public decimal? Large { get; set; }

        public decimal? LargeOrani { get; set; }

        public decimal? Jumbo { get; set; }

        public decimal? JumboOrani { get; set; }

        public decimal? SJumbo { get; set; }

        public decimal? SJumboOrani { get; set; }

        public decimal? CiftSari { get; set; }

        public decimal? CiftSariOrani { get; set; }

        public decimal? HayvanYasiHafta { get; set; }

        public decimal? CekimSuresiDk { get; set; }

        public decimal? SaatteCekilenYumurtaAdet { get; set; }

        public decimal? FiiliHayvanSayisi { get; set; }

        public decimal? FiiliYumurtlayanHayvanSayisi { get; set; }
    }
}