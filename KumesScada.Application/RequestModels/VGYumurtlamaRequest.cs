namespace KumesScada.Application.RequestModels
{
    public class VGYumurtlamaRequest
    {
        public string KumesNo { get; set; } = string.Empty;

        public string? Irk { get; set; }

        public DateTime KumeseGirisTarihi { get; set; }

        public decimal BaslangicHayvanSayisi { get; set; }

        public string? IcSiparisNo { get; set; }

        /// <summary>
        /// Hayvanın KumeseGirisTarihi günündeki yaşı (gün) — bkz.
        /// YumurtlamaDonem.GirisYasiGun. Belirtilmezse (null) GÜN, giriş
        /// tarihinde 1'den başlayan basit bir sayaç olur.
        /// </summary>
        public int? GirisYasiGun { get; set; }

        /// <summary>Bkz. YumurtlamaDonem.GirisHaftasi üzerindeki not —
        /// belirtilmezse CalculateWeek eskisi gibi
        /// CEILING(GirisYasiGun/7) formülüne düşer.</summary>
        public int? GirisHaftasi { get; set; }

        public List<VGYumurtlamaKaynakSatirRequest> Veriler { get; set; } = new();
    }

    public class VGYumurtlamaKaynakSatirRequest
    {
        public DateTime Tarih { get; set; }

        public decimal? GunlukOlum { get; set; }

        public decimal? KabukDirenci { get; set; }

        public decimal? GizliCatlak { get; set; }

        public decimal? SaglamYumurta { get; set; }

        public decimal? KirikYumurta { get; set; }

        public decimal? KirliYumurta { get; set; }

        public decimal? KucukKirik { get; set; }

        public decimal? Fire { get; set; }

        // Ana excel'in ("Yumurtlama Kartı") "Yumurtlama " sayfasında
        // sonradan eklenen sütunlar — eski kaynaklarda (Günlük Yem
        // Hesapları/Tasnif) hiç karşılığı yok, o yüzden hep null kalır.
        public decimal? LikidKabuksuzZar { get; set; }

        public decimal? ZarliYumurtaKg { get; set; }

        public decimal? MobaKirikDegeri { get; set; }

        public decimal? MobaKirliDegeri { get; set; }

        public string? KumesPersoneli { get; set; }

        public string? YeminRasyonNo { get; set; }

        /// <summary>
        /// Sağlam/Kırık/Kirli/Fire kırılımı hiç yoksa (toplamları 0
        /// çıkıyorsa) kullanılacak ham toplam yumurta adedi. Kırılımı
        /// olan kaynaklarda (ör. Tasnif Sayımları) bu alan boş bırakılır
        /// ve toplam kırılımdan hesaplanır.
        /// </summary>
        public decimal? ToplamYumurtaAdedi { get; set; }

        public decimal? YumurtaGramaji { get; set; }

        public decimal? CanliAgirlik { get; set; }

        public decimal? YemKg { get; set; }

        public decimal? SuLitre { get; set; }

        public decimal? Hu { get; set; }

        public decimal? TekSiraRandiman { get; set; }

        public decimal? VbapRandiman { get; set; }

        public string? Uygulama { get; set; }

        public decimal? Klavuz { get; set; }

        public decimal? Small { get; set; }

        public decimal? Medium { get; set; }

        public decimal? Large { get; set; }

        public decimal? Jumbo { get; set; }

        public decimal? SJumbo { get; set; }

        public decimal? CiftSari { get; set; }

        public decimal? HayvanYasiHafta { get; set; }

        public decimal? CekimSuresiDk { get; set; }

        public decimal? SaatteCekilenYumurtaAdet { get; set; }

        public decimal? FiiliHayvanSayisi { get; set; }

        public decimal? FiiliYumurtlayanHayvanSayisi { get; set; }
    }
}