namespace KumesScada.Application.RequestModels
{
    public sealed class VGYumurtlamaQueryRequest
    {
        public string KumesNo { get; set; } = string.Empty;

        /// <summary>
        /// Belirtilirse SADECE bu takvim yılının verileri getirilir (Grafik
        /// sekmesinin yıl seçicisi için). Boş bırakılırsa kümesin TÜM
        /// tarihçesi getirilir — bir parti birden fazla takvim yılına
        /// yayılabildiği için (bkz. veritabanından tam Yumurtlama Kartı
        /// excel'i üretimi, IVGYumurtlamaExcelGenerator.
        /// GenerateFromDatabaseAsync) tek yılla sınırlamak yanlış olur.
        /// </summary>
        public int? Year { get; set; }

        // Bunların DB tarafındaki nihai kaynağını sonra bağlarız.
        public DateTime KumeseGirisTarihi { get; set; }

        public decimal BaslangicHayvanSayisi { get; set; }

        public string? Irk { get; set; }

        public string? IcSiparisNo { get; set; }

        /// <summary>
        /// Hayvanın KumeseGirisTarihi günündeki yaşı (gün) — bkz.
        /// YumurtlamaDonem.GirisYasiGun. Belirtilmezse GÜN, giriş
        /// tarihinde 1'den başlayan basit bir sayaç olur.
        /// </summary>
        public int? GirisYasiGun { get; set; }

        /// <summary>Bkz. YumurtlamaDonem.GirisHaftasi üzerindeki not.</summary>
        public int? GirisHaftasi { get; set; }
    }
}