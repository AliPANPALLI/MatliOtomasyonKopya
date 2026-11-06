namespace KumesScada.Domain.Models.Excel
{
    public class VGExcelKumesGunlukData
    {
        public DateTime Tarih { get; set; }

        public string KumesNo { get; set; } = string.Empty;

        public decimal? Olum { get; set; }

        public decimal? YumurtaAdeti { get; set; }

        public decimal? YumurtaGramaji { get; set; }

        public decimal? CanliAgirlik { get; set; }

        // Kümes Tasnif Sayımları dosyasındaki "Ara Toplam" (1. blok:
        // Klavuz+Small+Medium+Large+Jumbo+S.Jumbo+Çift Sarı) toplamı.
        public decimal? SaglamYumurta { get; set; }

        // "200.409 Kırık" satırı.
        public decimal? KirikYumurta { get; set; }

        // "200.408 Kirli" satırı.
        public decimal? KirliYumurta { get; set; }

        // "200409 Zar Çatlağı" satırı -> Yumurtlama kartındaki
        // "Küçük Kırık" sütununun karşılığı olarak alınıyor.
        public decimal? KucukKirik { get; set; }

        // "200.693 Likid" satırı -> Yumurtlama kartındaki "Fire" sütunu.
        public decimal? Fire { get; set; }

        // Ana excel'in ("Yumurtlama Kartı") "Yumurtlama " sayfasında
        // sonradan eklenen "Likid - Kabuksuz Zar" sütunu (bkz.
        // VGYumurtlamaKartExcelReader) — eski kart biçiminde (ör.
        // "T1 127.PARTİ") bu sütun hiç yok, o dosyalarda null kalır.
        public decimal? LikidKabuksuzZar { get; set; }

        // Ana excel'in ("Yumurtlama Kartı") "Yumurtlama " sayfasında
        // sonradan eklenen "ZARLI YUMURTA (KG)" sütunu (bkz.
        // VGYumurtlamaKartExcelReader) — eski kart biçiminde hiç yok,
        // yeni kartlarda da ara sıra dolu (çoğu gün boş), null kalır.
        public decimal? ZarliYumurtaKg { get; set; }

        // Ana excel'in ("Yumurtlama Kartı") "Yumurtlama " sayfasında
        // sonradan eklenen "MOBA KIRIK DEĞERİ" / "MOBA KİRLİ DEĞERİ
        // (0,1,2,3)" sütunları (bkz. VGYumurtlamaKartExcelReader) —
        // eski kart biçiminde hiç yok, T14 135.PARTİ'de de şu an boş
        // (hiç veri girilmemiş), ileride dolabilir diye eklendi.
        public decimal? MobaKirikDegeri { get; set; }

        public decimal? MobaKirliDegeri { get; set; }

        // Günlük Yem Hesapları dosyasındaki "Kalite" (Gizli Çatlak) sayfası.
        public decimal? GizliCatlak { get; set; }

        // Ana excel'in ("Yumurtlama Kartı") "Yumurtlama " sayfasındaki
        // "Kabuk Direnci" sütunu (bkz. VGYumurtlamaKartExcelReader) —
        // Günlük Yem Hesapları/Tasnif dosyalarında şu an karşılığı yok.
        public decimal? KabukDirenci { get; set; }

        public decimal? YemTuketim { get; set; }

        public decimal? YemTuketimHayvanBasi { get; set; }

        public decimal? SuTuketim { get; set; }

        public decimal? Randiman { get; set; }

        public decimal? VbapRandiman { get; set; }

        public string? Uygulama { get; set; }

        // Ana excel'in "Yumurtlama " sayfasında sonradan eklenen serbest
        // metin sütunları — DİKKAT: T14 135.PARTİ kartında gerçek
        // içerikleri başlıklarıyla uyuşmuyor (ikisi de personel/rasyon
        // bilgisi değil, aydınlatma/yem saat aralığı notu içeriyor, bkz.
        // proje notları) — yine de kart neyse onu olduğu gibi saklıyoruz.
        public string? KumesPersoneli { get; set; }

        public string? YeminRasyonNo { get; set; }

        // Kümes Tasnif Sayımları dosyasındaki yumurta boyut sınıflandırması
        // satırları (adet) - "Ara Toplam" (SaglamYumurta) bunların toplamıdır.
        public decimal? Klavuz { get; set; }

        public decimal? Small { get; set; }

        public decimal? Medium { get; set; }

        public decimal? Large { get; set; }

        public decimal? Jumbo { get; set; }

        public decimal? SJumbo { get; set; }

        public decimal? CiftSari { get; set; }

        // Kümes Tasnif Sayımları dosyasındaki hayvan detay satırları.
        public decimal? HayvanYasiHafta { get; set; }

        public decimal? CekimSuresiDk { get; set; }

        public decimal? SaatteCekilenYumurtaAdet { get; set; }

        public decimal? FiiliHayvanSayisi { get; set; }

        public decimal? FiiliYumurtlayanHayvanSayisi { get; set; }
    }
}