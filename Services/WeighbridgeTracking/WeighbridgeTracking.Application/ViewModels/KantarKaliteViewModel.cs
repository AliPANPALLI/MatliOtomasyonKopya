
using WeighbridgeTracking.Domain.Enums;

namespace WeighbridgeTracking.Application.ViewModels
{
    // Teslim fişi - Kalite Kontrol Bilgileri
    // Zorunlu: KantarGuvenlikId, Nem. Diğerleri boş bırakılabilir.
    public class KantarKaliteViewModel
    {
        public int Id { get; set; }

        public int? KantarGuvenlikId { get; set; }

        // Kimyasal kontrol
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

        // 1 = Kabul, 2 = Şartlı Kabul, 3 = Red
        public KaliteSonucu? Sonuc { get; set; }
        public string? SonucAciklama { get; set; }

        // Kontrolü yapan personel; tamamlamadan önce seçilmiş olmalı.
        public int? KaliteKontrolPersonelId { get; set; }
    }
}
