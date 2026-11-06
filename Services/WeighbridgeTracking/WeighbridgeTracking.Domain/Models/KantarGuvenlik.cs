using System.Text.Json.Serialization;
using WeighbridgeTracking.Domain.Enums;

namespace WeighbridgeTracking.Domain.Models
{
    // Teslim fişi - Genel Kontroller
    public class KantarGuvenlik
    {
        public int Id { get; set; }

        // Ziyaretçi formundan gelen benzersiz takip kodu.
        // Ürün bu kod ile güvenlikten kaliteye kadar takip edilir.
        public string? ZiyaretciKayitId { get; set; }

        // Zorunlu alanlar: Plaka ve Tarih
        public string? Plaka { get; set; }

        // PlakaHelper.Normalize(Plaka); arama için indeksli.
        [JsonIgnore] public string? PlakaNormalized { get; set; }

        public DateTime Tarih { get; set; }

        public string? UrunAdi { get; set; }
        public int? FirmaId { get; set; }
        public string? IrsaliyeNo { get; set; }
        public string? SonUrun { get; set; }

        // Sistem giriş zamanı (KPI hesapları bunu kullanır).
        public DateTime? KayitZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
        public bool KaliteTamamlandi { get; set; } = false;

        public DateTime? KaliteTamamlanmaZamani { get; set; }

        public string? KaliteTamamlayanUserId { get; set; }

        // Kalite tamamlanırken son kalite kaydının kararı buraya yazılır.
        public KaliteSonucu? KaliteSonucu { get; set; }

        // Teslim fişi - Boşaltım Bilgileri
        // Kalite kararından sonra girilir; ayrı uç ile güncellenir.
        public string? DokulduguYer { get; set; }
        public DateTime? BosaltimZamani { get; set; }
        public string? BosaltimYapanUserId { get; set; }

        public bool? Silindi { get; set; }
        [JsonIgnore] public virtual Firma? Firma { get; set; }
        public ICollection<KantarKalite> KantarKaliteler { get; set; } = new List<KantarKalite>();
    }
}
