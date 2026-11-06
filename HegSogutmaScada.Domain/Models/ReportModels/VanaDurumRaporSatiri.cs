namespace HegSogutmaScada.Domain.Models.ReportModels
{
    // Bir vananın seçilen tarih aralığındaki açık/kapalı süresi.
    // AcikSaniye, vana_olay geçmişinden (vana_acik_saniye fonksiyonu ile) anlık hesaplanır.
    public class VanaDurumRaporSatiri
    {
        public short VanaId { get; set; }
        public short? OdaId { get; set; }
        public string? OdaAdi { get; set; }
        public string? OdaKat { get; set; }
        public string? EkipmanKodu { get; set; }
        public string? Aciklama { get; set; }

        public bool? GuncelDurumAcik { get; set; }
        public DateTime? GuncelDurumBaslangici { get; set; }

        public DateTime AralikBaslangic { get; set; }
        public DateTime AralikBitis { get; set; }

        // vana_acik_saniye() fonksiyonu veri bulunamadığında NULL dönebildiği için
        // bu alanlar da nullable - null, "bu aralık için veri yok" anlamına gelir.
        public decimal? AcikSaniye { get; set; }
        public decimal? KapaliSaniye { get; set; }
        public decimal? AcikYuzde { get; set; }
    }
}
