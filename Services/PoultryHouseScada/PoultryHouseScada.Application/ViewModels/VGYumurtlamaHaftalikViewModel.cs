namespace PoultryHouseScada.Application.ViewModels
{
    public class VGYumurtlamaHaftalikViewModel
    {
        public int Hafta { get; set; }

        public DateTime BaslangicTarihi { get; set; }

        public DateTime BitisTarihi { get; set; }

        public decimal? OrtalamaKabukDirenci { get; set; }

        public decimal? OrtalamaGizliCatlak { get; set; }

        public decimal ToplamOlum { get; set; }

        public decimal OlumOrani { get; set; }

        public decimal DonemSonuTavukSayisi { get; set; }

        public decimal ToplamSaglam { get; set; }

        public decimal SaglamOrani { get; set; }

        public decimal ToplamKirik { get; set; }

        public decimal KirikOrani { get; set; }

        public decimal ToplamKirli { get; set; }

        public decimal KirliOrani { get; set; }

        public decimal ToplamKucukKirik { get; set; }

        public decimal KucukKirikOrani { get; set; }

        public decimal ToplamFire { get; set; }

        public decimal FireOrani { get; set; }

        // Ana excel'in "Yumurtlama " sayfasında sonradan eklenen
        // sütunlar — eski kartlarda/kaynaklarda null kalır.
        public decimal? ToplamLikidKabuksuzZar { get; set; }

        public decimal? LikidKabuksuzZarOrani { get; set; }

        public decimal? ToplamZarliYumurtaKg { get; set; }

        public decimal? OrtalamaMobaKirikDegeri { get; set; }

        public decimal? OrtalamaMobaKirliDegeri { get; set; }

        public decimal ToplamYumurta { get; set; }

        public decimal HdRandiman { get; set; }

        public decimal HhRandiman { get; set; }

        public decimal? OrtalamaYumurtaGramaji { get; set; }

        public decimal? YumurtaKutlesi { get; set; }

        public decimal ToplamYemKg { get; set; }

        public decimal OrtalamaHayvanBasiYemGr { get; set; }

        public decimal ToplamSuLitre { get; set; }

        public decimal SuYemOrani { get; set; }

        public decimal? YumurtaBasiYemGr { get; set; }

        public decimal? OrtalamaTekSiraRandiman { get; set; }

        public decimal? OrtalamaVbapRandiman { get; set; }

        public decimal? ToplamKlavuz { get; set; }

        public decimal? KlavuzOrani { get; set; }

        public decimal? ToplamSmall { get; set; }

        public decimal? SmallOrani { get; set; }

        public decimal? ToplamMedium { get; set; }

        public decimal? MediumOrani { get; set; }

        public decimal? ToplamLarge { get; set; }

        public decimal? LargeOrani { get; set; }

        public decimal? ToplamJumbo { get; set; }

        public decimal? JumboOrani { get; set; }

        public decimal? ToplamSJumbo { get; set; }

        public decimal? SJumboOrani { get; set; }

        public decimal? ToplamCiftSari { get; set; }

        public decimal? CiftSariOrani { get; set; }

        public decimal? OrtalamaHayvanYasiHafta { get; set; }

        public decimal? OrtalamaCekimSuresiDk { get; set; }

        public decimal? OrtalamaSaatteCekilenYumurtaAdet { get; set; }

        public decimal? OrtalamaFiiliHayvanSayisi { get; set; }

        public decimal? OrtalamaFiiliYumurtlayanHayvanSayisi { get; set; }
    }
}