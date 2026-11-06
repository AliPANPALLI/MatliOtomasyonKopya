using System.Text.Json.Serialization;

namespace PoultryHouseScada.Application.ViewModels
{
    /// <summary>
    /// Verim Gıda'nın dış entegrasyonunun beklediği JSON paketi — tek bir
    /// kümesin tam "Yumurtlama Kartı" verisini (GÜN 1-7 + haftalık
    /// "Toplam" satırı, gerçek ana excel kartındaki "KRİTER" rotasyonu
    /// dahil) haftalara gruplanmış şekilde döner. Alan adları (snake_case)
    /// Verim Gıda'nın verdiği örnek paketle birebir eşleşecek şekilde
    /// JsonPropertyName ile sabitlenmiştir — değiştirilmemelidir.
    /// </summary>
    public class VGYumurtlamaGenelVeriViewModel
    {
        [JsonPropertyName("kumes")]
        public VGYumurtlamaGenelVeriKumesViewModel Kumes { get; set; } = new();

        [JsonPropertyName("genel_veri")]
        public VGYumurtlamaGenelVeriGenelViewModel GenelVeri { get; set; } = new();

        [JsonPropertyName("haftalar")]
        public List<VGYumurtlamaGenelVeriHaftaViewModel> Haftalar { get; set; } = new();
    }

    public class VGYumurtlamaGenelVeriKumesViewModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("ad")]
        public string Ad { get; set; } = string.Empty;
    }

    public class VGYumurtlamaGenelVeriGenelViewModel
    {
        /// <summary>İstenen kümesle AYNI lokasyondaki tüm VG kümesleri.</summary>
        [JsonPropertyName("kumesler")]
        public List<VGYumurtlamaGenelVeriKumesViewModel> Kumesler { get; set; } = new();

        [JsonPropertyName("irk")]
        public string? Irk { get; set; }

        /// <summary>dd.MM.yyyy formatında.</summary>
        [JsonPropertyName("kumese_giris_tarihi")]
        public string? KumeseGirisTarihi { get; set; }

        [JsonPropertyName("miktar")]
        public decimal Miktar { get; set; }
    }

    public class VGYumurtlamaGenelVeriHaftaViewModel
    {
        [JsonPropertyName("hafta")]
        public int Hafta { get; set; }

        [JsonPropertyName("gunler")]
        public List<VGYumurtlamaGenelVeriGunViewModel> Gunler { get; set; } = new();
    }

    public class VGYumurtlamaGenelVeriGunViewModel
    {
        /// <summary>Haftanın günü — Pazartesi=1 ... Pazar=7, haftalık
        /// "Toplam" satırı için son real günden sonraki sıra numarası
        /// (tam bir haftada 8).</summary>
        [JsonPropertyName("haftanin_gunu")]
        public int HaftaninGunu { get; set; }

        [JsonPropertyName("veriler")]
        public VGYumurtlamaGenelVeriVerilerViewModel Veriler { get; set; } = new();
    }

    public class VGYumurtlamaGenelVeriVerilerViewModel
    {
        [JsonPropertyName("gun")]
        public int? Gun { get; set; }

        [JsonPropertyName("hafta")]
        public int Hafta { get; set; }

        /// <summary>dd.MM.yyyy formatında — haftalık "Toplam" satırında null.</summary>
        [JsonPropertyName("tarih")]
        public string? Tarih { get; set; }

        /// <summary>
        /// Gerçek ana excel kartındaki "KRİTER" sütunu — haftanın belirli
        /// günlerinde (Çar/Per/Cum/Cmt/Paz) dönüşümlü olarak Randıman(%)/
        /// Yem (gr)/Toplam Yem/Adet/Yaşama oranı(%) etiketlerini taşır,
        /// diğer günlerde boştur. Karşılığı "hafta_degeri" alanındadır.
        /// </summary>
        [JsonPropertyName("kriter")]
        public string Kriter { get; set; } = string.Empty;

        [JsonPropertyName("kabuk_direnci")]
        public decimal? KabukDirenci { get; set; }

        [JsonPropertyName("gizli_catlak")]
        public decimal? GizliCatlak { get; set; }

        /// <summary>
        /// "kriter" boş değilse o haftanın ilgili özet değeri (sayısal);
        /// haftalık "Toplam" satırında sabit "Toplam" metni; aksi halde null.
        /// </summary>
        [JsonPropertyName("hafta_degeri")]
        public object? HaftaDegeri { get; set; }

        [JsonPropertyName("gunluk_olum")]
        public decimal GunlukOlum { get; set; }

        [JsonPropertyName("olum_yuzde")]
        public decimal OlumYuzde { get; set; }

        [JsonPropertyName("tavuk_sayisi")]
        public decimal TavukSayisi { get; set; }

        [JsonPropertyName("yumurta_saglam")]
        public decimal YumurtaSaglam { get; set; }

        [JsonPropertyName("yumurta_saglam_yuzde")]
        public decimal YumurtaSaglamYuzde { get; set; }

        [JsonPropertyName("yumurta_kirik")]
        public decimal YumurtaKirik { get; set; }

        [JsonPropertyName("yumurta_kirik_yuzde")]
        public decimal YumurtaKirikYuzde { get; set; }

        [JsonPropertyName("yumurta_kirli")]
        public decimal YumurtaKirli { get; set; }

        [JsonPropertyName("yumurta_kirli_yuzde")]
        public decimal YumurtaKirliYuzde { get; set; }

        [JsonPropertyName("yumurta_kucuk_kirik")]
        public decimal YumurtaKucukKirik { get; set; }

        [JsonPropertyName("yumurta_kucuk_kirik_yuzde")]
        public decimal YumurtaKucukKirikYuzde { get; set; }

        [JsonPropertyName("yumurta_fire")]
        public decimal YumurtaFire { get; set; }

        [JsonPropertyName("yumurta_fire_yuzde")]
        public decimal YumurtaFireYuzde { get; set; }

        /// <summary>Ana excel kartının güncel (T14 135.PARTİ ve sonrası)
        /// terminolojisiyle "yumurta_kucuk_kirik"in TEKRARI — aynı alan,
        /// çift sayılmaz (bkz. VGYumurtlamaExcelGenerator'daki aynı not).</summary>
        [JsonPropertyName("yumurta_zar_catlagi")]
        public decimal YumurtaZarCatlagi { get; set; }

        [JsonPropertyName("yumurta_zar_catlagi_yuzde")]
        public decimal YumurtaZarCatlagiYuzde { get; set; }

        /// <summary>"yumurta_fire"in güncel kart terminolojisiyle TEKRARI.</summary>
        [JsonPropertyName("yumurta_likit")]
        public decimal YumurtaLikit { get; set; }

        [JsonPropertyName("yumurta_likit_yuzde")]
        public decimal YumurtaLikitYuzde { get; set; }

        /// <summary>Gerçekten YENİ bir alan (Fire'ın alt kırılımı) — eski
        /// kart biçiminde (T1 127.PARTİ gibi) hiç yok, null kalır.</summary>
        [JsonPropertyName("yumurta_likit_kabuksuz")]
        public decimal? YumurtaLikitKabuksuz { get; set; }

        [JsonPropertyName("yumurta_likit_kabuksuz_yuzde")]
        public decimal? YumurtaLikitKabuksuzYuzde { get; set; }

        /// <summary>Eski kart biçiminde hiç yok, yeni kartlarda da ara
        /// sıra elle doldurulan bir sütun — bkz. VGExcelKumesGunlukData.
        /// ZarliYumurtaKg.</summary>
        [JsonPropertyName("zarli_yumurta_kg")]
        public decimal? ZarliYumurtaKg { get; set; }

        [JsonPropertyName("yumurta_toplam")]
        public decimal YumurtaToplam { get; set; }

        [JsonPropertyName("hd_yuzde")]
        public decimal HdYuzde { get; set; }

        [JsonPropertyName("hh_yuzde")]
        public decimal HhYuzde { get; set; }

        /// <summary>Haftalık toplam yumurta / hafta sonu tavuk sayısı — haftanın tüm
        /// günlerinde (ve Toplam satırında) aynı haftalık değer tekrarlanır.</summary>
        [JsonPropertyName("haftalik_yumurta_tavuk_basina")]
        public decimal? HaftalikYumurtaTavukBasina { get; set; }

        [JsonPropertyName("tavuk_basina_yumurta_toplami")]
        public decimal? TavukBasinaYumurtaToplami { get; set; }

        [JsonPropertyName("egg_gr")]
        public decimal? EggGr { get; set; }

        [JsonPropertyName("gunluk_yumurta_kutlesi")]
        public decimal? GunlukYumurtaKutlesi { get; set; }

        [JsonPropertyName("yem_kg")]
        public decimal YemKg { get; set; }

        [JsonPropertyName("hayvan_basi_yem_gr")]
        public decimal HayvanBasiYemGr { get; set; }

        [JsonPropertyName("su_lt")]
        public decimal SuLt { get; set; }

        [JsonPropertyName("su_yem")]
        public decimal SuYem { get; set; }

        [JsonPropertyName("yumurta_basi_yem")]
        public decimal? YumurtaBasiYem { get; set; }

        [JsonPropertyName("hu")]
        public decimal? Hu { get; set; }

        [JsonPropertyName("uygulamalar")]
        public string Uygulamalar { get; set; } = string.Empty;

        /// <summary>Ana excel kartının "Yumurtlama " sayfasındaki
        /// "KÜMES PERSONELİ" sütunu — DİKKAT: gerçek içeriği başlıkla
        /// uyuşmuyor (bkz. VGExcelKumesGunlukData.KumesPersoneli), kart
        /// neyse onu olduğu gibi taşır. Eski kart biçiminde hiç yok.</summary>
        [JsonPropertyName("kumes_personeli")]
        public string KumesPersoneli { get; set; } = string.Empty;

        /// <summary>Ana excel kartının "Yemin rasyon no" sütunu — aynı
        /// şekilde içeriği başlıkla uyuşmuyor, bkz. KumesPersoneli notu.</summary>
        [JsonPropertyName("yem_rasyon_no")]
        public string YemRasyonNo { get; set; } = string.Empty;

        /// <summary>Eski kart biçiminde hiç yok, yeni kartlarda da şu an
        /// hiç veri girilmemiş (bkz. VGExcelKumesGunlukData.MobaKirikDegeri) —
        /// bulunamazsa null.</summary>
        [JsonPropertyName("moba_kirik_degeri")]
        public decimal? MobaKirikDegeri { get; set; }

        /// <summary>Bkz. MobaKirikDegeri notu.</summary>
        [JsonPropertyName("moba_kirli_degeri")]
        public decimal? MobaKirliDegeri { get; set; }

        /// <summary>Sadece haftalık "Toplam" satırında "toplam" — günlük satırlarda alan hiç yazılmaz.</summary>
        [JsonPropertyName("_satir_tipi")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SatirTipi { get; set; }
    }
}
