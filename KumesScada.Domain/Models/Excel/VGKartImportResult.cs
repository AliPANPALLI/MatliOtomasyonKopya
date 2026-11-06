namespace KumesScada.Domain.Models.Excel
{
    /// <summary>
    /// VGYumurtlamaKartExcelReader'ın "Yumurtlama " sayfasından okuduğu
    /// günlük veriler + kapak bilgisinden (varsa) türetilen dönem
    /// bilgisi (giriş tarihi/başlangıç hayvan sayısı/ırk/girişte yaş).
    /// Kapak bilgisi, "Geçmiş Veri Yükle" akışında dönem bilgisini
    /// elle girmeye gerek kalmadan otomatik kaydetmek için kullanılır.
    /// </summary>
    public class VGKartImportResult
    {
        public List<VGExcelKumesGunlukData> GunlukVeriler { get; set; } = new();

        public DateTime? GirisTarihi { get; set; }

        public decimal? BaslangicHayvanSayisi { get; set; }

        public string? Irk { get; set; }

        /// <summary>
        /// GirisTarihi günü, sayfadaki GÜN satırından okunan değer —
        /// gerçek kartta GÜN, kümese giriş tarihinden değil hayvanın
        /// yaşından başladığı için (bkz. YumurtlamaDonem.GirisYasiGun),
        /// kapak satırındaki TARİH, sayfanın GÜN sütununda hangi satıra
        /// denk geliyorsa o GÜN değeri budur.
        /// </summary>
        public int? GirisYasiGun { get; set; }

        /// <summary>Bkz. YumurtlamaDonem.GirisHaftasi üzerindeki not.</summary>
        public int? GirisHaftasi { get; set; }

        /// <summary>
        /// Sabit sütun pozisyonundan (10/12/14/16/18) okunan Sağlam/Kırık/
        /// Kirli/Küçük Kırık("Zar Çatlağı")/FİRE("Likid") alanlarının
        /// başlık metni BEKLENEN isimlerden biri değilse buraya eklenir
        /// (bkz. VGYumurtlamaKartExcelReader.BeklenenEtiketKontrolu) —
        /// okuma pozisyona güvenir, başlık metnine bakmaz; bu uyarı kart
        /// biçimi üçüncü kez değiştiğinde sessiz yanlış okumayı yakalar.
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
}
