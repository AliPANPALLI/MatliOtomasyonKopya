using WeighbridgeTracking.Domain.Enums;

namespace WeighbridgeTracking.Application.DTOs
{
    // API yanıtları için. Alan adları entity ile aynı tutuldu
    // (istemci uyumluluğu); yüklenmeyen navigation koleksiyonları yok.

    public sealed class FirmaDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool? Silindi { get; set; }
    }

    public sealed class KaliteKontrolPersonelDto
    {
        public int Id { get; set; }
        public string? AdSoyad { get; set; }
        public bool? Silindi { get; set; }
    }

    public sealed class KantarGuvenlikDto
    {
        public int Id { get; set; }
        public string? ZiyaretciKayitId { get; set; }
        public string? Plaka { get; set; }
        public DateTime Tarih { get; set; }
        public string? UrunAdi { get; set; }
        public int? FirmaId { get; set; }
        public string? IrsaliyeNo { get; set; }
        public string? SonUrun { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
        public bool KaliteTamamlandi { get; set; }
        public DateTime? KaliteTamamlanmaZamani { get; set; }
        public string? KaliteTamamlayanUserId { get; set; }

        // 1 = Kabul, 2 = Şartlı Kabul, 3 = Red (kalite tamamlanınca dolar)
        public KaliteSonucu? KaliteSonucu { get; set; }

        public string? DokulduguYer { get; set; }
        public DateTime? BosaltimZamani { get; set; }
        public string? BosaltimYapanUserId { get; set; }

        public bool? Silindi { get; set; }
    }

    public sealed class KantarKaliteDto
    {
        public int Id { get; set; }
        public int? KantarGuvenlikId { get; set; }

        public decimal? Nem { get; set; }
        public decimal? Protein { get; set; }
        public decimal? Kul { get; set; }
        public decimal? Seluloz { get; set; }
        public decimal? Yag { get; set; }
        public decimal? Nisasta { get; set; }
        public decimal? Koh { get; set; }
        public decimal? Ureaz { get; set; }

        public string? Koku { get; set; }
        public string? Tozluluk { get; set; }
        public string? Curuk { get; set; }
        public string? Kuf { get; set; }
        public string? Boceklenme { get; set; }

        public KaliteSonucu? Sonuc { get; set; }
        public string? SonucAciklama { get; set; }

        public int? KaliteKontrolPersonelId { get; set; }

        public DateTime? KayitZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
        public bool? Silindi { get; set; }
    }

    // ZiyaretciKayitId ile ürün takibi: genel kontrol + kalite kayıtları.
    public sealed class ZiyaretTakipDto
    {
        public KantarGuvenlikDto Guvenlik { get; set; } = null!;
        public List<KantarKaliteDto> KaliteKayitlari { get; set; } = new();
    }
}
