
using System;
using System.Text.Json.Serialization;
using KantarTakip.Domain.Enums;

namespace KantarTakip.Domain.Models
{
    // Teslim fişi - Kalite Kontrol Bilgileri
    public class KantarKalite
    {
        public int Id { get; set; }

        public int? KantarGuvenlikId { get; set; }

        // Kimyasal kontrol (Nem zorunlu)
        public decimal? Nem { get; set; }
        public decimal? Protein { get; set; }
        public decimal? Kul { get; set; }
        public decimal? Seluloz { get; set; }
        public decimal? Yag { get; set; }
        public decimal? Nisasta { get; set; }
        public decimal? Koh { get; set; }
        public decimal? Ureaz { get; set; }

        // Fiziki kontrol
        public string? Koku { get; set; }
        public string? Tozluluk { get; set; }
        public string? Curuk { get; set; }
        public string? Kuf { get; set; }
        public string? Boceklenme { get; set; }

        // Kabul / Şartlı Kabul / Red
        public KaliteSonucu? Sonuc { get; set; }
        public string? SonucAciklama { get; set; }

        // Kontrolü yapan personel (kayıt sonrası da seçilebilir).
        public int? KaliteKontrolPersonelId { get; set; }

        public DateTime? KayitZamani { get; set; }

        public string? GuncelleyenUserId { get; set; }

        public bool? Silindi { get; set; }

        [JsonIgnore]
        public virtual KantarGuvenlik? KantarGuvenlik { get; set; }

        [JsonIgnore]
        public virtual KaliteKontrolPersonel? KaliteKontrolPersonel { get; set; }
    }
}
