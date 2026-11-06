namespace SharedApp.Domain.Models
{
    public class MesajGonderimTakip
    {
        public int Id { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? SmsIcerik { get; set; }
        public bool? GonderimBasarili { get; set; }
        public string? HataMesaji { get; set; }
        public string? TelefonNo { get; set; }
        public DateTime? SonDenemeZamani { get; set; }
        public short? KalanDenemeSayisi { get; set; }
        public string? UserId { get; set; }
        public string? KumesAlarmLogIds { get; set; }
        public string? KonveyorAlarmLogIds { get; set; }
        public string? SuHatAlarmLogIds { get; set; }
        public int? SeviyeNo { get; set; }
        public string? SesDosyaYolu { get; set; }          // çalınacak ses dosyası (TTS de olabilir)
        public int? AramaSure { get; set; }                // konuşma süresi (sn)
        public bool? CevaplandiMi { get; set; }            // çağrıya cevap verildi mi?


        // --- Netgsm raporlama için yeni alanlar ---
        public string? CallId { get; set; }        // Netgsm’den dönen arama ID
        public string? DurumKodu { get; set; }     // Netgsm rapor kodu
        public byte? Durum { get; set; }     // Netgsm rapor kodu
        public string? DurumAciklama { get; set; } // Netgsm rapor açıklaması
        public DateTime? BaslamaZamani { get; set; }
        public DateTime? BitisZamani { get; set; }
        public int? DinlemeSuresi { get; set; }    // cevaplandıysa kaç sn dinlendi
    }
}