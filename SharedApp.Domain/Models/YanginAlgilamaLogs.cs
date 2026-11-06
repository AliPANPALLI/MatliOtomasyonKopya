namespace SharedApp.Domain.Models
{
    public class YanginAlgilamaLogs
    {
        public int Id { get; set; }
        public string? AlarmTip { get; set; }
        public string? KameraNo { get; set; }
        public DateTime? KayitZamani { get; set; }
        public DateTime? NodeRedKayitZamani { get; set; }
        /*
         * Ana klasör hariç göreli yol tutulur.
         * Örnek: 2026\07\16\f13d8a.jpg
         */
        public string? AnlikFotografDosyaYolu { get; set; }
        public string? AnlikFotografDosyaAdi { get; set; }
        public string? AnlikFotografContentType { get; set; }
        public long? AnlikFotografBoyutu { get; set; }
        public bool? Silindi { get; set; }
        public int? AlarmLogId { get; set; }
        public virtual AlarmLog? AlarmLog { get; set; }

    }
}